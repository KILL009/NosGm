using NosGm.Authentication.Client.Configuration;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.Master.Library.Data;
using System;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class ConfigurationServiceClient
    {
        #region Instantiation

        public ConfigurationServiceClient()
        {
            _rollbackTransport =
                ConfigurationRollbackTransportFactory.Create(
                    OnConfigurationUpdated);

            string authorityDiagnostic;
            if (ConfigurationAuthorityQualificationRuntime.Instance
                    .TryConfigureFromEnvironment(
                        out authorityDiagnostic))
            {
                ConfigurationAuthorityStatus authorityStatus =
                    ConfigurationAuthorityCoordinator.Instance.GetStatus();
                if (authorityStatus.EffectRoutingEnabled)
                {
                    Logger.Warn(
                        "[CONFIG_GRPC_AUTHORITY] Joint Get/Update/callback " +
                        "effect routing was explicitly requested; SCS remains " +
                        "authoritative until qualification, activation, and " +
                        "typed snapshot recovery all complete.");
                }
                else
                {
                    Logger.Info(
                        "[CONFIG_GRPC_AUTHORITY] Lifecycle observation enabled in " +
                        authorityDiagnostic +
                        " mode; SCS remains the immutable production authority.");
                }
            }
            else
            {
                Logger.Warn(
                    "[CONFIG_GRPC_AUTHORITY] Operator controls failed closed; " +
                    "SCS remains authoritative. Reason=" +
                    (authorityDiagnostic ?? "unknown"));
            }

            string shadowDiagnostic;
            ConfigurationGrpcShadowMirror shadowMirror;
            if (ConfigurationGrpcShadowMirror.TryCreateFromEnvironment(
                    out shadowMirror,
                    out shadowDiagnostic))
            {
                _grpcShadowMirror = shadowMirror;
                Logger.Info(
                    "[CONFIG_GRPC_SHADOW] Configuration shadow mirror enabled; SCS remains authoritative.");
            }
            else if (!string.Equals(
                         shadowDiagnostic,
                         "disabled",
                         StringComparison.Ordinal))
            {
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] Shadow mirror unavailable; continuing with SCS authority. Reason=" +
                    (shadowDiagnostic ?? "unknown"));
            }

            string subscriberDiagnostic;
            ConfigurationGrpcShadowSubscriberLifecycle subscriberLifecycle;
            if (ConfigurationGrpcShadowSubscriberLifecycle
                    .TryStartFromEnvironment(
                        OnTypedConfigurationUpdated,
                        out subscriberLifecycle,
                        out subscriberDiagnostic))
            {
                _grpcShadowSubscriberLifecycle = subscriberLifecycle;
                Logger.Info(
                    "[CONFIG_GRPC_SHADOW] Typed Configuration update subscriber started; " +
                    "the joint selector remains on SCS until every cutover barrier passes.");
            }
            else if (!string.Equals(
                         subscriberDiagnostic,
                         "disabled",
                         StringComparison.Ordinal))
            {
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] Typed update subscriber unavailable; continuing with SCS callback authority. Reason=" +
                    (subscriberDiagnostic ?? "unknown"));
            }

            ConfigurationAuthorityStatus finalAuthorityStatus =
                ConfigurationAuthorityCoordinator.Instance.GetStatus();
            if (finalAuthorityStatus.EffectRoutingEnabled &&
                (_grpcShadowMirror == null ||
                 _grpcShadowSubscriberLifecycle == null))
            {
                RollBackAuthority("startup", null);
            }
            ConfigurationAuthorityDiagnostics.Observe("STARTUP");
        }

        #endregion

        #region Events

        public event EventHandler ConfigurationUpdate;

        #endregion

        #region Members

        private static ConfigurationServiceClient _instance;

        private const int AcceptancePulseTimeoutMilliseconds = 7000;

        private const int AcceptancePulseRestoreAttempts = 3;

        private const string AcceptancePulseEnabledEnvironmentVariable =
            "NOSGM_CONFIGURATION_GRPC_ACCEPTANCE_PULSE_ENABLED";

        private int _acceptancePulseRunning;

        private static readonly object AcceptancePulseIsolationRoot =
            new object();

        private readonly object _configurationMutationRoot = new object();

        private readonly IConfigurationRollbackTransport
            _rollbackTransport;

        private readonly ConfigurationGrpcShadowMirror _grpcShadowMirror;

        private readonly ConfigurationGrpcShadowSubscriberLifecycle
            _grpcShadowSubscriberLifecycle;

        #endregion

        #region Properties

        public static ConfigurationServiceClient Instance =>
            _instance ?? (_instance = new ConfigurationServiceClient());

        #endregion

        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            return _rollbackTransport.Authenticate(authKey, serverId);
        }

        public ConfigurationObject GetConfigurationObject()
        {
            ConfigurationAuthorityCoordinator authority =
                ConfigurationAuthorityCoordinator.Instance;
            if (authority.ShouldUse(
                    ConfigurationAuthoritySource.TypedGrpc,
                    ConfigurationAuthorityOperation.Get))
            {
                ConfigurationGrpcShadowResult typedResult = null;
                ConfigurationObject typedConfiguration;
                if (_grpcShadowMirror != null &&
                    _grpcShadowMirror.TryGetAuthoritative(
                        out typedConfiguration,
                        out typedResult) &&
                    IsCurrentAuthorityResult(typedResult))
                {
                    return typedConfiguration;
                }

                RollBackAuthority("Get", typedResult);
            }

            ConfigurationObject authoritative =
                _rollbackTransport.GetConfigurationObject();
            ObserveAuthoritativeConfiguration(authoritative, "Get");
            return authoritative;
        }

        public void UpdateConfigurationObject(ConfigurationObject configurationObject)
        {
            lock (AcceptancePulseIsolationRoot)
            {
                lock (_configurationMutationRoot)
                {
                    UpdateConfigurationObjectCore(configurationObject);
                }
            }
        }

        public static void RunWithConfigurationMutationBarrier(
            Action mutation)
        {
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            lock (AcceptancePulseIsolationRoot)
            {
                mutation();
            }
        }

        public bool TryRunGrpcAcceptancePulse(
            ConfigurationObject liveWorldConfiguration,
            Func<bool> isolationStillValid,
            out string diagnostic)
        {
            diagnostic = null;
            if (!string.Equals(
                    Environment.GetEnvironmentVariable(
                        AcceptancePulseEnabledEnvironmentVariable),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                diagnostic = "acceptance-pulse-disabled";
                return false;
            }
            if (liveWorldConfiguration == null)
            {
                diagnostic = "live-world-configuration-unavailable";
                return false;
            }
            if (isolationStillValid == null)
            {
                diagnostic = "isolation-check-unavailable";
                return false;
            }
            if (_grpcShadowMirror == null ||
                _grpcShadowSubscriberLifecycle == null)
            {
                diagnostic = "shadow-unavailable";
                return false;
            }
            if (Interlocked.CompareExchange(
                    ref _acceptancePulseRunning,
                    1,
                    0) != 0)
            {
                diagnostic = "pulse-already-running";
                return false;
            }

            try
            {
                lock (AcceptancePulseIsolationRoot)
                {
                    bool isolationLost = false;
                    bool restorationVerified = true;
                    ConfigurationObject original;
                    ConfigurationUpdateParityReport before;
                    lock (_configurationMutationRoot)
                    {
                        if (!isolationStillValid())
                        {
                            diagnostic = "world-not-isolated";
                            return false;
                        }

                        ConfigurationObject liveWorld =
                            CloneConfiguration(liveWorldConfiguration);
                        DateTime worldNow = DateTime.Now;
                        if (liveWorld.TimeExpBuff > worldNow ||
                            liveWorld.TimeGoldBuff > worldNow)
                        {
                            diagnostic = "active-world-buff";
                            return false;
                        }

                        original = _rollbackTransport
                            .GetConfigurationObject();
                        if (original == null || original.MaxGold <= 0)
                        {
                            diagnostic = "configuration-unavailable";
                            return false;
                        }
                        if (!ConfigurationsAreExactlyEqual(
                                liveWorld,
                                original))
                        {
                            diagnostic = "live-world-configuration-drift";
                            return false;
                        }
                        if (original.MaxGold == long.MaxValue)
                        {
                            diagnostic = "max-gold-pulse-unavailable";
                            return false;
                        }

                        ConfigurationObject typedOriginal;
                        ConfigurationGrpcShadowResult typedOriginalResult;
                        if (!_grpcShadowMirror.TryGetAuthoritative(
                                out typedOriginal,
                                out typedOriginalResult) ||
                            !IsCurrentAuthorityResult(typedOriginalResult))
                        {
                            diagnostic = "typed-runtime-unavailable";
                            return false;
                        }
                        if (!ConfigurationsAreSemanticallyEqual(
                                original,
                                typedOriginal))
                        {
                            diagnostic = "typed-configuration-drift";
                            return false;
                        }

                        before = ConfigurationUpdateObservationLedger.Instance
                            .LatestParityReport;
                        if (string.IsNullOrWhiteSpace(
                                before.RuntimeGenerationId) ||
                            !string.Equals(
                                before.RuntimeGenerationId,
                                typedOriginalResult.RuntimeGenerationId,
                                StringComparison.Ordinal))
                        {
                            diagnostic = "typed-runtime-unavailable";
                            return false;
                        }
                        if (before.HasTerminalMismatch)
                        {
                            diagnostic = "terminal-parity-" + before.Verdict;
                            return false;
                        }
                        if (!isolationStillValid())
                        {
                            diagnostic = "world-not-isolated";
                            return false;
                        }

                        ConfigurationObject pulse =
                            CreateGrpcAcceptancePulse(original);
                        bool pulseAttempted = false;
                        try
                        {
                            Logger.Info(
                                "[CONFIG_GRPC_ACCEPTANCE_PULSE] Stage=STARTED " +
                                "Runtime=" + before.RuntimeGenerationId +
                                "; no Configuration values are logged.");
                            pulseAttempted = true;
                            UpdateAcceptanceSnapshotEverywhere(pulse);
                        }
                        finally
                        {
                            if (pulseAttempted)
                            {
                                restorationVerified =
                                    TryRestoreAcceptanceSnapshotEverywhere(
                                        original);
                            }
                        }
                    }

                    if (!restorationVerified)
                    {
                        diagnostic = "restoration-verification-failed";
                        LogAcceptancePulseResult(
                            "FAILED",
                            before,
                            ConfigurationUpdateObservationLedger.Instance
                                .LatestParityReport,
                            false);
                        return false;
                    }

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(
                        AcceptancePulseTimeoutMilliseconds);
                    while (DateTime.UtcNow < deadline)
                    {
                        if (!isolationStillValid())
                        {
                            isolationLost = true;
                        }

                        ConfigurationUpdateParityReport after =
                            ConfigurationUpdateObservationLedger.Instance
                                .LatestParityReport;
                        if (!string.Equals(
                                before.RuntimeGenerationId,
                                after.RuntimeGenerationId,
                                StringComparison.Ordinal))
                        {
                            diagnostic = "runtime-changed";
                            LogAcceptancePulseResult(
                                "REJECTED",
                                before,
                                after,
                                true);
                            return false;
                        }
                        if (after.HasTerminalMismatch)
                        {
                            diagnostic = "terminal-parity-" + after.Verdict;
                            LogAcceptancePulseResult(
                                "REJECTED",
                                before,
                                after,
                                true);
                            return false;
                        }
                        if (after.HasParity &&
                            after.ScsLiveCount >= before.ScsLiveCount + 2 &&
                            after.GrpcLiveCount >= before.GrpcLiveCount + 2 &&
                            after.MatchedLiveCount >=
                                before.MatchedLiveCount + 2 &&
                            ConfigurationsAreExactlyEqual(
                                liveWorldConfiguration,
                                original))
                        {
                            diagnostic = isolationLost
                                ? "world-isolation-lost"
                                : "pass";
                            LogAcceptancePulseResult(
                                isolationLost ? "REJECTED" : "PASS",
                                before,
                                after,
                                true);
                            return !isolationLost;
                        }
                        Thread.Sleep(25);
                    }

                    diagnostic = ConfigurationsAreExactlyEqual(
                        liveWorldConfiguration,
                        original)
                        ? "parity-timeout"
                        : "live-world-restore-timeout";
                    LogAcceptancePulseResult(
                        "TIMEOUT",
                        before,
                        ConfigurationUpdateObservationLedger.Instance
                            .LatestParityReport,
                        true);
                    return false;
                }
            }
            catch (Exception exception)
            {
                diagnostic = "pulse-failed-" +
                    exception.GetType().Name;
                Logger.Error(
                    "[CONFIG_GRPC_ACCEPTANCE_PULSE] Stage=FAILED; " +
                    "every changed Configuration store was restored and " +
                    "verified when reachable, " +
                    "and no Configuration values were logged.",
                    exception);
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref _acceptancePulseRunning, 0);
            }
        }

        private void UpdateConfigurationObjectCore(
            ConfigurationObject configurationObject)
        {
            if (configurationObject == null)
            {
                throw new ArgumentNullException(nameof(configurationObject));
            }
            ConfigurationAuthorityCoordinator authority =
                ConfigurationAuthorityCoordinator.Instance;
            if (authority.ShouldUse(
                    ConfigurationAuthoritySource.TypedGrpc,
                    ConfigurationAuthorityOperation.Update))
            {
                ConfigurationGrpcShadowResult typedResult = null;
                if (_grpcShadowMirror != null &&
                    _grpcShadowMirror.TryUpdateAuthoritative(
                        configurationObject,
                        out typedResult) &&
                    IsCurrentAuthorityResult(typedResult))
                {
                    SynchronizeScsStandby(
                        configurationObject,
                        typedResult);
                    return;
                }

                RollBackAuthority("Update", typedResult);
            }

            _rollbackTransport.UpdateConfigurationObject(
                configurationObject);
            ObserveAuthoritativeConfiguration(configurationObject, "Update");
        }

        private static ConfigurationObject CreateGrpcAcceptancePulse(
            ConfigurationObject original)
        {
            ConfigurationObject pulse = CloneConfiguration(original);
            pulse.MaxGold = checked(pulse.MaxGold + 1);
            return pulse;
        }

        private void UpdateAcceptanceSnapshotEverywhere(
            ConfigurationObject configuration)
        {
            _rollbackTransport.UpdateConfigurationObject(
                CloneConfiguration(configuration));
            ConfigurationGrpcShadowResult typedResult;
            if (!_grpcShadowMirror.TryUpdateAuthoritative(
                    CloneConfiguration(configuration),
                    out typedResult) ||
                !IsCurrentAuthorityResult(typedResult))
            {
                throw new InvalidOperationException(
                    "The typed Configuration acceptance write failed closed.");
            }
        }

        private bool TryRestoreAcceptanceSnapshotEverywhere(
            ConfigurationObject original)
        {
            for (int attempt = 0;
                 attempt < AcceptancePulseRestoreAttempts;
                 attempt++)
            {
                try
                {
                    _rollbackTransport.UpdateConfigurationObject(
                        CloneConfiguration(original));
                }
                catch (Exception)
                {
                    // Verification below decides whether another attempt is
                    // required without exposing Configuration values.
                }

                ConfigurationGrpcShadowResult typedUpdateResult;
                _grpcShadowMirror.TryUpdateAuthoritative(
                    CloneConfiguration(original),
                    out typedUpdateResult);

                ConfigurationObject scsCurrent = null;
                try
                {
                    scsCurrent = _rollbackTransport
                        .GetConfigurationObject();
                }
                catch (Exception)
                {
                    // A failed read cannot verify restoration.
                }

                ConfigurationObject typedCurrent;
                ConfigurationGrpcShadowResult typedGetResult;
                bool typedVerified =
                    _grpcShadowMirror.TryGetAuthoritative(
                        out typedCurrent,
                        out typedGetResult) &&
                    IsCurrentAuthorityResult(typedGetResult) &&
                    ConfigurationsAreSemanticallyEqual(
                        original,
                        typedCurrent);
                if (ConfigurationsAreExactlyEqual(original, scsCurrent) &&
                    typedVerified)
                {
                    return true;
                }

                Thread.Sleep(25);
            }

            return false;
        }

        private static bool ConfigurationsAreExactlyEqual(
            ConfigurationObject left,
            ConfigurationObject right)
        {
            return left != null &&
                   right != null &&
                   left.MaxGold == right.MaxGold &&
                   left.TimeExpBuff.Ticks == right.TimeExpBuff.Ticks &&
                   left.TimeExpBuff.Kind == right.TimeExpBuff.Kind &&
                   left.TimeGoldBuff.Ticks == right.TimeGoldBuff.Ticks &&
                   left.TimeGoldBuff.Kind == right.TimeGoldBuff.Kind;
        }

        private static bool ConfigurationsAreSemanticallyEqual(
            ConfigurationObject left,
            ConfigurationObject right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            ConfigurationTransportSnapshot leftSnapshot =
                ConfigurationGrpcShadowMirror.ToTransportSnapshot(left);
            ConfigurationTransportSnapshot rightSnapshot =
                ConfigurationGrpcShadowMirror.ToTransportSnapshot(right);
            return leftSnapshot.MaxGold == rightSnapshot.MaxGold &&
                   leftSnapshot.TimeExpBuffUnixTimeMilliseconds ==
                       rightSnapshot.TimeExpBuffUnixTimeMilliseconds &&
                   leftSnapshot.TimeGoldBuffUnixTimeMilliseconds ==
                       rightSnapshot.TimeGoldBuffUnixTimeMilliseconds;
        }

        private static ConfigurationObject CloneConfiguration(
            ConfigurationObject source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new ConfigurationObject
            {
                MaxGold = source.MaxGold,
                TimeExpBuff = source.TimeExpBuff,
                TimeGoldBuff = source.TimeGoldBuff
            };
        }

        private static void LogAcceptancePulseResult(
            string stage,
            ConfigurationUpdateParityReport before,
            ConfigurationUpdateParityReport after,
            bool restored)
        {
            int scsDelta = Math.Max(
                0,
                after.ScsLiveCount - before.ScsLiveCount);
            int grpcDelta = Math.Max(
                0,
                after.GrpcLiveCount - before.GrpcLiveCount);
            int matchedDelta = Math.Max(
                0,
                after.MatchedLiveCount - before.MatchedLiveCount);
            Logger.Info(
                "[CONFIG_GRPC_ACCEPTANCE_PULSE] Stage=" + stage +
                " Runtime=" + after.RuntimeGenerationId +
                " ScsDelta=" + scsDelta +
                " GrpcDelta=" + grpcDelta +
                " MatchedDelta=" + matchedDelta +
                " Restored=" + restored +
                "; no Configuration values are logged.");
        }

        internal void OnConfigurationUpdated(ConfigurationObject configurationObject)
        {
            ObserveScsConfigurationCallback(configurationObject);
            ConfigurationTransportSnapshot snapshot =
                ConfigurationGrpcShadowMirror.ToTransportSnapshot(
                    configurationObject);
            ConfigurationAuthorityCoordinator.Instance.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                snapshot,
                () => ConfigurationUpdate?.Invoke(
                    configurationObject,
                    EventArgs.Empty));
        }

        private bool OnTypedConfigurationUpdated(
            ConfigurationTransportUpdate update)
        {
            ConfigurationAuthorityCoordinator authority =
                ConfigurationAuthorityCoordinator.Instance;
            if (!authority.ShouldUse(
                    ConfigurationAuthoritySource.TypedGrpc,
                    ConfigurationAuthorityOperation.Callback))
            {
                return false;
            }
            if (_grpcShadowMirror == null)
            {
                RollBackAuthority("Callback", null);
                return false;
            }

            ConfigurationObject configuration =
                ConfigurationGrpcShadowMirror.FromTransportSnapshot(
                    update.Configuration);
            return authority.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                update.Configuration,
                () => ConfigurationUpdate?.Invoke(
                    configuration,
                    EventArgs.Empty));
        }

        private static bool IsCurrentAuthorityResult(
            ConfigurationGrpcShadowResult result)
        {
            ConfigurationAuthorityStatus status =
                ConfigurationAuthorityCoordinator.Instance.GetStatus();
            return result != null &&
                   result.Generation > 0 &&
                   string.Equals(
                       result.RuntimeGenerationId,
                       status.ActiveRuntimeGenerationId,
                       StringComparison.Ordinal);
        }

        private void SynchronizeScsStandby(
            ConfigurationObject configuration,
            ConfigurationGrpcShadowResult typedResult)
        {
            try
            {
                _rollbackTransport.UpdateConfigurationObject(
                    configuration);
                Logger.Info(
                    "[CONFIG_GRPC_AUTHORITY] Typed Update generation " +
                    typedResult.Generation +
                    " synchronized the SCS rollback standby.");
            }
            catch (Exception exception)
            {
                var rollback = new InvalidOperationException(
                    "The SCS Configuration rollback standby could not be synchronized after a typed Update.",
                    exception);
                ConfigurationAuthorityCoordinator.Instance.RequestRollback(
                    rollback);
                ConfigurationAuthorityDiagnostics.Observe(
                    "STANDBY_SYNC_FAILED");
                Logger.Error(
                    "[CONFIG_GRPC_AUTHORITY] SCS rollback standby synchronization " +
                    "failed after a typed Update; typed authority was closed for " +
                    "this process.",
                    exception);
                throw;
            }
        }

        private static void RollBackAuthority(
            string operation,
            ConfigurationGrpcShadowResult result)
        {
            string resultName = result == null
                ? "unavailable"
                : result.Status + "/" + result.TransportResult;
            var exception = new InvalidOperationException(
                "Typed Configuration " + operation +
                " authority failed with " + resultName + ".");
            ConfigurationAuthorityCoordinator.Instance.RequestRollback(
                exception);
            ConfigurationAuthorityDiagnostics.Observe(
                "AUTHORITY_ROLLBACK");
            Logger.Warn(
                "[CONFIG_GRPC_AUTHORITY] Typed " + operation +
                " failed closed; all Configuration operations returned to " +
                "SCS for this process. Reason=" + resultName);
        }

        private static void ObserveScsConfigurationCallback(
            ConfigurationObject configurationObject)
        {
            try
            {
                ConfigurationUpdateObservationLedger ledger =
                    ConfigurationUpdateObservationLedger.Instance;
                ledger.RecordScs(
                    ConfigurationGrpcShadowMirror.ToTransportSnapshot(
                        configurationObject));
                ConfigurationUpdateParityReport report =
                    ledger.LatestParityReport;
                ConfigurationUpdateParityDiagnostics.Observe(report);
                ConfigurationAuthorityQualificationRuntime.Instance
                    .ObserveParity(report);
                ConfigurationAuthorityDiagnostics.Observe("SCS_PARITY");
            }
            catch (Exception exception)
            {
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] SCS callback observation failed; " +
                    "the authoritative callback will continue unchanged. Reason=" +
                    exception.GetType().Name);
            }
        }

        private void ObserveAuthoritativeConfiguration(
            ConfigurationObject authoritative,
            string source)
        {
            if (_grpcShadowMirror == null)
            {
                return;
            }

            ConfigurationGrpcShadowResult result =
                _grpcShadowMirror.Synchronize(authoritative);
            switch (result.Status)
            {
                case ConfigurationGrpcShadowStatus.Matched:
                    return;
                case ConfigurationGrpcShadowStatus.Seeded:
                case ConfigurationGrpcShadowStatus.Resynchronized:
                    Logger.Info(
                        "[CONFIG_GRPC_SHADOW] " + source +
                        " synchronized shadow generation " +
                        result.Generation + ". SCS remains authoritative.");
                    return;
                default:
                    Logger.Warn(
                        "[CONFIG_GRPC_SHADOW] " + source +
                        " shadow observation failed with " +
                        result.Status + "/" + result.TransportResult +
                        "; SCS result remains authoritative.");
                    return;
            }
        }

        #endregion
    }
}
