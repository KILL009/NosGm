using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace NosGm.Authentication.Client.Configuration
{
    public enum ConfigurationUpdateCursorDecision
    {
        Accepted = 0,
        Duplicate = 1,
        Gap = 2,
        RuntimeChanged = 3,
        Stale = 4,
        Invalid = 5
    }

    public sealed class ConfigurationUpdateCursor
    {
        private readonly object _syncRoot = new object();
        private ulong _generation;
        private string _runtimeGenerationId = string.Empty;

        public ulong Generation
        {
            get
            {
                lock (_syncRoot)
                {
                    return _generation;
                }
            }
        }

        public string RuntimeGenerationId
        {
            get
            {
                lock (_syncRoot)
                {
                    return _runtimeGenerationId;
                }
            }
        }

        public ConfigurationUpdateCursorDecision Inspect(
            ConfigurationTransportUpdate update,
            bool allowSnapshotRecovery)
        {
            if (!IsValid(update))
            {
                return ConfigurationUpdateCursorDecision.Invalid;
            }

            lock (_syncRoot)
            {
                if (string.IsNullOrEmpty(_runtimeGenerationId))
                {
                    return ConfigurationUpdateCursorDecision.Accepted;
                }
                if (!string.Equals(
                        _runtimeGenerationId,
                        update.RuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    return allowSnapshotRecovery
                        ? ConfigurationUpdateCursorDecision.Accepted
                        : ConfigurationUpdateCursorDecision.RuntimeChanged;
                }
                if (update.Generation < _generation)
                {
                    return ConfigurationUpdateCursorDecision.Stale;
                }
                if (update.Generation == _generation)
                {
                    return ConfigurationUpdateCursorDecision.Duplicate;
                }
                if (!allowSnapshotRecovery &&
                    update.Generation != checked(_generation + 1UL))
                {
                    return ConfigurationUpdateCursorDecision.Gap;
                }

                return ConfigurationUpdateCursorDecision.Accepted;
            }
        }

        public void Commit(ConfigurationTransportUpdate update)
        {
            if (!IsValid(update))
            {
                throw new ArgumentException(
                    "The Configuration update cursor cannot commit an invalid update.",
                    nameof(update));
            }

            lock (_syncRoot)
            {
                _runtimeGenerationId = update.RuntimeGenerationId;
                _generation = update.Generation;
            }
        }

        private static bool IsValid(ConfigurationTransportUpdate update)
        {
            return update != null &&
                   update.Configuration != null &&
                   update.Generation > 0 &&
                   Guid.TryParseExact(
                       update.RuntimeGenerationId,
                       "D",
                       out Guid parsed) &&
                   string.Equals(
                       update.RuntimeGenerationId,
                       parsed.ToString("D"),
                       StringComparison.Ordinal);
        }
    }

    public sealed class ConfigurationGrpcShadowSubscriberOptions
    {
        public const int DefaultInitialReconnectDelayMilliseconds = 250;
        public const int DefaultMaximumReconnectDelayMilliseconds = 5000;

        public int InitialReconnectDelayMilliseconds { get; set; } =
            DefaultInitialReconnectDelayMilliseconds;

        public int MaximumReconnectDelayMilliseconds { get; set; } =
            DefaultMaximumReconnectDelayMilliseconds;

        internal void Validate()
        {
            if (InitialReconnectDelayMilliseconds < 50 ||
                InitialReconnectDelayMilliseconds > 10000)
            {
                throw new InvalidOperationException(
                    "The Configuration subscriber initial reconnect delay must be between 50 and 10000 milliseconds.");
            }
            if (MaximumReconnectDelayMilliseconds <
                    InitialReconnectDelayMilliseconds ||
                MaximumReconnectDelayMilliseconds > 60000)
            {
                throw new InvalidOperationException(
                    "The Configuration subscriber maximum reconnect delay must be between the initial delay and 60000 milliseconds.");
            }
        }
    }

    public interface IConfigurationGrpcShadowSubscriberRunner : IDisposable
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    public sealed class ConfigurationGrpcShadowSubscriber
        : IConfigurationGrpcShadowSubscriberRunner,
          IClusterConfigurationUpdateHandler
    {
        private readonly ConfigurationUpdateCursor _cursor =
            new ConfigurationUpdateCursor();
        private readonly IDisposable _disposableSnapshotTransport;
        private readonly IDisposable _disposableStreamTransport;
        private readonly IClusterConfigurationUpdateHandler _observer;
        private readonly ConfigurationGrpcShadowSubscriberOptions _options;
        private readonly IClusterConfigurationTransport _snapshotTransport;
        private readonly IClusterConfigurationUpdateStreamTransport
            _streamTransport;
        private int _disposed;
        private int _madeStreamProgress;
        private int _running;

        public ConfigurationGrpcShadowSubscriber(
            IClusterConfigurationTransport snapshotTransport,
            IClusterConfigurationUpdateStreamTransport streamTransport,
            IClusterConfigurationUpdateHandler observer,
            ConfigurationGrpcShadowSubscriberOptions options = null)
        {
            _snapshotTransport = snapshotTransport ??
                throw new ArgumentNullException(nameof(snapshotTransport));
            _streamTransport = streamTransport ??
                throw new ArgumentNullException(nameof(streamTransport));
            _observer = observer ??
                throw new ArgumentNullException(nameof(observer));
            _options = options ??
                new ConfigurationGrpcShadowSubscriberOptions();
            _options.Validate();
            _disposableSnapshotTransport = snapshotTransport as IDisposable;
            if (!ReferenceEquals(snapshotTransport, streamTransport))
            {
                _disposableStreamTransport = streamTransport as IDisposable;
            }
        }

        public ulong AppliedGeneration => _cursor.Generation;

        public string RuntimeGenerationId => _cursor.RuntimeGenerationId;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                throw new InvalidOperationException(
                    "The Configuration shadow subscriber is already running.");
            }

            int reconnectDelay =
                _options.InitialReconnectDelayMilliseconds;
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool recovered = false;
                    Volatile.Write(ref _madeStreamProgress, 0);
                    try
                    {
                        recovered = await RecoverSnapshotAsync(cancellationToken)
                            .ConfigureAwait(false);
                        await _streamTransport.SubscribeUpdatesAsync(
                                _cursor.RuntimeGenerationId,
                                _cursor.Generation,
                                this,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                        when (IsReconnectable(exception))
                    {
                        // Recovery below is snapshot based. No SCS result or
                        // gameplay Configuration object is changed here.
                    }

                    bool madeProgress = recovered ||
                        Interlocked.Exchange(ref _madeStreamProgress, 0) != 0;
                    reconnectDelay = madeProgress
                        ? _options.InitialReconnectDelayMilliseconds
                        : Math.Min(
                            _options.MaximumReconnectDelayMilliseconds,
                            checked(reconnectDelay * 2));
                    await Task.Delay(reconnectDelay, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                Volatile.Write(ref _running, 0);
            }
        }

        public async Task ObserveAsync(
            ConfigurationTransportUpdate update,
            CancellationToken cancellationToken)
        {
            ConfigurationUpdateCursorDecision decision =
                _cursor.Inspect(update, allowSnapshotRecovery: false);
            if (decision == ConfigurationUpdateCursorDecision.Duplicate)
            {
                return;
            }
            if (decision != ConfigurationUpdateCursorDecision.Accepted)
            {
                throw new RpcException(
                    new Status(
                        decision == ConfigurationUpdateCursorDecision.Gap
                            ? StatusCode.OutOfRange
                            : StatusCode.FailedPrecondition,
                        "The Configuration update cursor requires snapshot recovery: " +
                        decision + "."));
            }

            await _observer.ObserveAsync(update, cancellationToken)
                .ConfigureAwait(false);
            _cursor.Commit(update);
            Volatile.Write(ref _madeStreamProgress, 1);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _disposableStreamTransport?.Dispose();
            _disposableSnapshotTransport?.Dispose();
        }

        private async Task<bool> RecoverSnapshotAsync(
            CancellationToken cancellationToken)
        {
            ConfigurationTransportResult snapshot =
                await _snapshotTransport.GetAsync(cancellationToken)
                    .ConfigureAwait(false);
            if (snapshot == null ||
                snapshot.Result == ConfigurationTransportResultCode.Unavailable)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.Unavailable,
                        "The Configuration shadow snapshot is unavailable."));
            }
            if (snapshot.Result != ConfigurationTransportResultCode.Success ||
                snapshot.Configuration == null)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration shadow snapshot was rejected."));
            }

            var recovered = new ConfigurationTransportUpdate
            {
                Configuration = snapshot.Configuration,
                Generation = snapshot.Generation,
                RuntimeGenerationId = snapshot.RuntimeGenerationId,
                RecoveredFromSnapshot = true
            };
            ConfigurationUpdateCursorDecision decision =
                _cursor.Inspect(recovered, allowSnapshotRecovery: true);
            if (decision == ConfigurationUpdateCursorDecision.Duplicate)
            {
                return false;
            }
            if (decision != ConfigurationUpdateCursorDecision.Accepted)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.DataLoss,
                        "The Configuration recovery snapshot is invalid or stale: " +
                        decision + "."));
            }

            await _observer.ObserveAsync(recovered, cancellationToken)
                .ConfigureAwait(false);
            _cursor.Commit(recovered);
            return true;
        }

        private static bool IsReconnectable(Exception exception)
        {
            if (exception is RpcException rpc)
            {
                return rpc.StatusCode != StatusCode.PermissionDenied &&
                       rpc.StatusCode != StatusCode.Unauthenticated &&
                       rpc.StatusCode != StatusCode.InvalidArgument;
            }

            return exception is HttpRequestException ||
                   exception is IOException ||
                   exception is TimeoutException ||
                   exception is OperationCanceledException;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(ConfigurationGrpcShadowSubscriber));
            }
        }
    }
}
