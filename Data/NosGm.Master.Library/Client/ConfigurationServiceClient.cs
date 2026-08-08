using NosGm.Authentication.Client.Configuration;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Configuration;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class ConfigurationServiceClient : IConfigurationService
    {
        #region Instantiation

        public ConfigurationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _confClient = new ConfigurationClient();
            _client = ScsServiceClientBuilder.CreateClient<IConfigurationService>(new ScsTcpEndPoint(ip, port),
                _confClient);
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: nameof(CommunicationServiceClient));
                    Thread.Sleep(1000);
                }

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
        }

        #endregion

        #region Events

        public event EventHandler ConfigurationUpdate;

        #endregion

        #region Members

        private static ConfigurationServiceClient _instance;

        private readonly IScsServiceClient<IConfigurationService> _client;

        private readonly ConfigurationClient _confClient;

        private readonly ConfigurationGrpcShadowMirror _grpcShadowMirror;

        private readonly ConfigurationGrpcShadowSubscriberLifecycle
            _grpcShadowSubscriberLifecycle;

        #endregion

        #region Properties

        public static ConfigurationServiceClient Instance =>
            _instance ?? (_instance = new ConfigurationServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            return _client.ServiceProxy.Authenticate(authKey, serverId);
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
                _client.ServiceProxy.GetConfigurationObject();
            ObserveAuthoritativeConfiguration(authoritative, "Get");
            return authoritative;
        }

        public void UpdateConfigurationObject(ConfigurationObject configurationObject)
        {
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

            _client.ServiceProxy.UpdateConfigurationObject(configurationObject);
            ObserveAuthoritativeConfiguration(configurationObject, "Update");
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
                _client.ServiceProxy.UpdateConfigurationObject(configuration);
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
