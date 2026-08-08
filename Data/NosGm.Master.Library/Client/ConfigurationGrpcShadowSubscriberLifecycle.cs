using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;
using NosGm.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    internal static class ConfigurationUpdateParityDiagnostics
    {
        private static readonly object SyncRoot = new object();
        private static string _lastProcessGenerationId = string.Empty;
        private static string _lastRuntimeGenerationId = string.Empty;
        private static ulong _lastEvaluatedThroughLedgerOrdinal;
        private static ConfigurationUpdateParityVerdict _lastVerdict;

        internal static void Observe(ConfigurationUpdateParityReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            lock (SyncRoot)
            {
                bool sameProcess = string.Equals(
                    _lastProcessGenerationId,
                    report.ProcessGenerationId,
                    StringComparison.Ordinal);
                if (sameProcess &&
                    report.EvaluatedThroughLedgerOrdinal <
                        _lastEvaluatedThroughLedgerOrdinal)
                {
                    return;
                }
                if (sameProcess &&
                    string.Equals(
                        _lastRuntimeGenerationId,
                        report.RuntimeGenerationId,
                        StringComparison.Ordinal) &&
                    _lastEvaluatedThroughLedgerOrdinal ==
                        report.EvaluatedThroughLedgerOrdinal &&
                    _lastVerdict == report.Verdict)
                {
                    return;
                }

                _lastProcessGenerationId = report.ProcessGenerationId;
                _lastRuntimeGenerationId = report.RuntimeGenerationId;
                _lastEvaluatedThroughLedgerOrdinal =
                    report.EvaluatedThroughLedgerOrdinal;
                _lastVerdict = report.Verdict;
            }

            string diagnostic =
                "[CONFIG_GRPC_PARITY] Verdict=" + report.Verdict +
                " Runtime=" + report.RuntimeGenerationId +
                " Through=" + report.EvaluatedThroughLedgerOrdinal +
                " WindowStart=" + report.WindowStartLedgerOrdinal +
                " ScsLive=" + report.ScsLiveCount +
                " GrpcLive=" + report.GrpcLiveCount +
                " Matched=" + report.MatchedLiveCount +
                " Recovery=" + report.GrpcRecoveryCount +
                " Replay=" + report.GrpcReplayCount +
                " Evicted=" + report.EvictedObservations +
                "; authority selection is evaluated separately and this evidence has no direct gameplay effect.";
            if (report.HasTerminalMismatch)
            {
                Logger.Warn(diagnostic);
            }
            else
            {
                Logger.Info(diagnostic);
            }
        }
    }

    internal sealed class ConfigurationGrpcShadowUpdateObserver
        : IClusterConfigurationUpdateHandler,
          IConfigurationGrpcShadowStreamLifecycleObserver
    {
        private readonly Func<ConfigurationTransportUpdate, bool>
            _authorityCallback;

        internal ConfigurationGrpcShadowUpdateObserver(
            Func<ConfigurationTransportUpdate, bool> authorityCallback)
        {
            _authorityCallback = authorityCallback ??
                throw new ArgumentNullException(nameof(authorityCallback));
        }

        public Task ObserveAsync(
            ConfigurationTransportUpdate update,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string phase = update.RecoveredFromSnapshot
                ? "recovery"
                : update.Replayed
                    ? "replay"
                    : "live";
            try
            {
                ConfigurationUpdateObservationLedger ledger =
                    ConfigurationUpdateObservationLedger.Instance;
                ledger.RecordGrpc(update);
                ConfigurationUpdateParityReport report =
                    ledger.LatestParityReport;
                ConfigurationUpdateParityDiagnostics.Observe(report);
                ConfigurationAuthorityQualificationRuntime.Instance
                    .ObserveParity(report);
                ConfigurationAuthorityQualificationRuntime.Instance
                    .ObserveTypedUpdate(
                        ledger.ProcessGenerationId,
                        update);
                bool applied = _authorityCallback(update);
                Logger.Info(
                    applied
                        ? "[CONFIG_GRPC_AUTHORITY] Applied typed Configuration update " +
                          "generation " + update.Generation + " during " + phase + "."
                        : "[CONFIG_GRPC_SHADOW] Observed typed Configuration update " +
                          "generation " + update.Generation + " during " + phase +
                          "; the current authority selector applied no typed gameplay effect.");
            }
            catch (Exception exception)
            {
                ConfigurationAuthorityQualificationRuntime.Instance
                    .Invalidate(exception);
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] Typed Configuration observation " +
                    "or selected callback failed closed to SCS; " +
                    "the subscriber will continue only as diagnostic input. Reason=" +
                    exception.GetType().Name);
            }
            return Task.CompletedTask;
        }

        public void ObserveStreamEnded(
            string runtimeGenerationId,
            Exception reason)
        {
            ConfigurationAuthorityQualificationRuntime.Instance
                .ObserveStreamEnded(runtimeGenerationId, reason);
        }
    }

    internal sealed class ConfigurationGrpcShadowSubscriberLifecycle
        : IDisposable
    {
        private readonly IConfigurationGrpcShadowSubscriberRunner _subscriber;
        private CancellationTokenSource _cancellation;
        private Task _runTask;
        private int _disposed;
        private int _started;

        private ConfigurationGrpcShadowSubscriberLifecycle(
            IConfigurationGrpcShadowSubscriberRunner subscriber)
        {
            _subscriber = subscriber ??
                throw new ArgumentNullException(nameof(subscriber));
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        internal static bool TryStartFromEnvironment(
            Func<ConfigurationTransportUpdate, bool> authorityCallback,
            out ConfigurationGrpcShadowSubscriberLifecycle lifecycle,
            out string diagnostic)
        {
            lifecycle = null;
            diagnostic = null;
            GrpcClusterConfigurationTransport transport = null;
            try
            {
                ConfigurationGrpcShadowOptions shadowOptions =
                    ConfigurationGrpcShadowOptions.Load();
                if (!shadowOptions.Enabled)
                {
                    diagnostic = "disabled";
                    return false;
                }

                AuthenticationGrpcClientOptions clientOptions =
                    AuthenticationGrpcClientOptions.Load(ClusterNodeRole.World);
                transport = new GrpcClusterConfigurationTransport(clientOptions);
                var subscriber = new ConfigurationGrpcShadowSubscriber(
                    transport,
                    transport,
                    new ConfigurationGrpcShadowUpdateObserver(
                        authorityCallback));
                lifecycle = new ConfigurationGrpcShadowSubscriberLifecycle(
                    subscriber);
                transport = null;
                lifecycle.Start();
                diagnostic = "enabled";
                return true;
            }
            catch (Exception exception)
            {
                transport?.Dispose();
                lifecycle?.Dispose();
                lifecycle = null;
                diagnostic = exception.GetType().Name;
                return false;
            }
        }

        private void Start()
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            {
                throw new InvalidOperationException(
                    "The Configuration shadow subscriber lifecycle can start only once.");
            }

            _cancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellation.Token;
            _runTask = Task.Run(async () =>
            {
                try
                {
                    await _subscriber.RunAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    // Expected process shutdown.
                }
                catch (Exception exception)
                {
                    ConfigurationAuthorityQualificationRuntime.Instance
                        .Invalidate(exception);
                    Logger.Warn(
                        "[CONFIG_GRPC_SHADOW] Typed Configuration subscriber stopped; " +
                        "SCS callback authority is unchanged. Reason=" +
                        exception.GetType().Name);
                }
            });
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cancellation?.Cancel();
            try
            {
                _runTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.Count == 1 &&
                      exception.InnerException is OperationCanceledException)
            {
                // Expected controlled shutdown.
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                _subscriber.Dispose();
                _cancellation?.Dispose();
            }
        }

        private void OnProcessExit(object sender, EventArgs eventArgs)
        {
            Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(ConfigurationGrpcShadowSubscriberLifecycle));
            }
        }
    }
}
