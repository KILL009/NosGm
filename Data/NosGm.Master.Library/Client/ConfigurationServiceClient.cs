using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;
using NosGm.Core;
using NosGm.Master.Library.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    public sealed class ConfigurationServiceClient : IDisposable
    {
        private static readonly object MutationRoot = new object();
        private static ConfigurationServiceClient _instance;

        private readonly GrpcClusterConfigurationTransport _transport;
        private readonly ConfigurationGrpcShadowSubscriber _subscriber;
        private readonly CancellationTokenSource _subscriberCancellation;
        private readonly Task _subscriberTask;
        private int _disposed;

        private ConfigurationServiceClient()
        {
            AuthenticationGrpcClientOptions options =
                AuthenticationGrpcClientOptions.Load(ClusterNodeRole.World);
            _transport = new GrpcClusterConfigurationTransport(options);
            _subscriber = new ConfigurationGrpcShadowSubscriber(
                _transport,
                _transport,
                new AuthoritativeConfigurationUpdateHandler(this));
            _subscriberCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken =
                _subscriberCancellation.Token;
            _subscriberTask = Task.Run(async () =>
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
                    Logger.Error(
                        "[CONFIG_GRPC] The authoritative Configuration subscriber stopped. " +
                        "No SCS fallback is available; operator intervention is required.",
                        exception);
                }
            });

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Logger.Info(
                "[CONFIG_GRPC] Configuration authority initialized in mandatory gRPC mode; " +
                "SCS fallback is disabled.");
        }

        public event EventHandler ConfigurationUpdate;

        public static ConfigurationServiceClient Instance =>
            _instance ?? (_instance = new ConfigurationServiceClient());

        public bool Authenticate(string authKey, Guid serverId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(authKey) || serverId == Guid.Empty)
            {
                return false;
            }

            try
            {
                GetConfigurationObject();
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CONFIG_GRPC] Configuration readiness probe failed closed. " +
                    "mTLS is the authoritative authentication mechanism.",
                    exception);
                return false;
            }
        }

        public ConfigurationObject GetConfigurationObject()
        {
            ThrowIfDisposed();
            ConfigurationTransportResult result = _transport
                .GetAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            EnsureSuccess(result, "Get");
            return FromTransportSnapshot(result.Configuration);
        }

        public void UpdateConfigurationObject(
            ConfigurationObject configurationObject)
        {
            ThrowIfDisposed();
            if (configurationObject == null)
            {
                throw new ArgumentNullException(nameof(configurationObject));
            }

            ConfigurationTransportResult result = _transport
                .UpdateAsync(
                    ToTransportSnapshot(configurationObject),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            EnsureSuccess(result, "Update");
        }

        public static void RunWithConfigurationMutationBarrier(Action mutation)
        {
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            lock (MutationRoot)
            {
                mutation();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _subscriberCancellation.Cancel();
            try
            {
                _subscriberTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.Count == 1 &&
                      exception.InnerException is OperationCanceledException)
            {
                // Expected controlled shutdown.
            }
            finally
            {
                _subscriber.Dispose();
                _subscriberCancellation.Dispose();
            }
        }

        private void OnAuthoritativeConfigurationUpdate(
            ConfigurationTransportUpdate update)
        {
            if (update == null || update.Configuration == null)
            {
                throw new InvalidOperationException(
                    "The authoritative Configuration stream returned an empty update.");
            }

            ConfigurationObject configuration =
                FromTransportSnapshot(update.Configuration);
            ConfigurationUpdate?.Invoke(configuration, EventArgs.Empty);
            Logger.Info(
                "[CONFIG_GRPC] Applied authoritative Configuration generation " +
                update.Generation +
                (update.RecoveredFromSnapshot
                    ? " from snapshot recovery."
                    : update.Replayed
                        ? " from replay."
                        : " from the live stream."));
        }

        private static void EnsureSuccess(
            ConfigurationTransportResult result,
            string operation)
        {
            if (result == null ||
                result.Result != ConfigurationTransportResultCode.Success ||
                result.Configuration == null ||
                result.Generation == 0 ||
                string.IsNullOrWhiteSpace(result.RuntimeGenerationId))
            {
                string resultName = result == null
                    ? "no-response"
                    : result.Result.ToString();
                throw new InvalidOperationException(
                    "Configuration gRPC " + operation +
                    " failed closed with result " + resultName +
                    "; no SCS fallback exists.");
            }
        }

        private static ConfigurationTransportSnapshot ToTransportSnapshot(
            ConfigurationObject configuration)
        {
            return new ConfigurationTransportSnapshot
            {
                MaxGold = configuration.MaxGold,
                TimeExpBuffUnixTimeMilliseconds =
                    ToUnixTimeMilliseconds(configuration.TimeExpBuff),
                TimeGoldBuffUnixTimeMilliseconds =
                    ToUnixTimeMilliseconds(configuration.TimeGoldBuff)
            };
        }

        private static ConfigurationObject FromTransportSnapshot(
            ConfigurationTransportSnapshot configuration)
        {
            return new ConfigurationObject
            {
                MaxGold = configuration.MaxGold,
                TimeExpBuff = DateTimeOffset.FromUnixTimeMilliseconds(
                        configuration.TimeExpBuffUnixTimeMilliseconds)
                    .LocalDateTime,
                TimeGoldBuff = DateTimeOffset.FromUnixTimeMilliseconds(
                        configuration.TimeGoldBuffUnixTimeMilliseconds)
                    .LocalDateTime
            };
        }

        private static long ToUnixTimeMilliseconds(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(value);
                return new DateTimeOffset(value, localOffset)
                    .ToUnixTimeMilliseconds();
            }

            return new DateTimeOffset(value).ToUnixTimeMilliseconds();
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
                    nameof(ConfigurationServiceClient));
            }
        }

        private sealed class AuthoritativeConfigurationUpdateHandler
            : IClusterConfigurationUpdateHandler,
              IConfigurationGrpcShadowStreamLifecycleObserver
        {
            private readonly ConfigurationServiceClient _owner;

            internal AuthoritativeConfigurationUpdateHandler(
                ConfigurationServiceClient owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public Task ObserveAsync(
                ConfigurationTransportUpdate update,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _owner.OnAuthoritativeConfigurationUpdate(update);
                return Task.CompletedTask;
            }

            public void ObserveStreamEnded(
                string runtimeGenerationId,
                Exception reason)
            {
                Logger.Warn(
                    "[CONFIG_GRPC] Authoritative Configuration stream ended for runtime " +
                    (runtimeGenerationId ?? "unknown") +
                    "; reconnect/recovery will remain gRPC-only. Reason=" +
                    (reason == null ? "stream-completed" : reason.GetType().Name));
            }
        }
    }
}
