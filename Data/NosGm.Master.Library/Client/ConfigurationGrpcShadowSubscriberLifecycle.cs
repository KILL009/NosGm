using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;
using NosGm.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    internal sealed class ConfigurationGrpcShadowUpdateObserver
        : IClusterConfigurationUpdateHandler
    {
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
                ConfigurationUpdateObservationLedger.Instance.RecordGrpc(update);
            }
            catch (Exception exception)
            {
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] Typed Configuration observation " +
                    "could not enter the parity ledger; " +
                    "the subscriber will continue without applying gameplay state. Reason=" +
                    exception.GetType().Name);
            }
            Logger.Info(
                "[CONFIG_GRPC_SHADOW] Observed typed Configuration update " +
                "generation " + update.Generation + " during " + phase +
                "; SCS callback remains authoritative and no gameplay state was applied.");
            return Task.CompletedTask;
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
                    new ConfigurationGrpcShadowUpdateObserver());
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
