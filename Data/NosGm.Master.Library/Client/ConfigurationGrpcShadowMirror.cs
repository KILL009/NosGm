using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;
using NosGm.Master.Library.Data;
using System;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    internal enum ConfigurationGrpcShadowStatus
    {
        Matched = 0,
        Seeded = 1,
        Resynchronized = 2,
        TimedOut = 3,
        Unavailable = 4,
        Faulted = 5
    }

    internal sealed class ConfigurationGrpcShadowResult
    {
        public ConfigurationGrpcShadowStatus Status { get; set; }

        public ulong Generation { get; set; }

        public ConfigurationTransportResultCode TransportResult { get; set; }
    }

    internal sealed class ConfigurationGrpcShadowMirror : IDisposable
    {
        private readonly IDisposable _disposableTransport;
        private readonly IClusterConfigurationTransport _transport;
        private readonly int _timeoutMilliseconds;
        private int _disposed;

        internal ConfigurationGrpcShadowMirror(
            IClusterConfigurationTransport transport,
            int timeoutMilliseconds)
        {
            _transport = transport ??
                throw new ArgumentNullException(nameof(transport));
            if (timeoutMilliseconds < 100 || timeoutMilliseconds > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            }

            _timeoutMilliseconds = timeoutMilliseconds;
            _disposableTransport = transport as IDisposable;
        }

        internal static bool TryCreateFromEnvironment(
            out ConfigurationGrpcShadowMirror mirror,
            out string diagnostic)
        {
            mirror = null;
            diagnostic = null;
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
                var transport =
                    new GrpcClusterConfigurationTransport(clientOptions);
                mirror = new ConfigurationGrpcShadowMirror(
                    transport,
                    shadowOptions.TimeoutMilliseconds);
                diagnostic = "enabled";
                return true;
            }
            catch (Exception exception)
            {
                diagnostic = exception.GetType().Name;
                return false;
            }
        }

        internal ConfigurationGrpcShadowResult Synchronize(
            ConfigurationObject authoritative)
        {
            ThrowIfDisposed();
            if (authoritative == null)
            {
                return new ConfigurationGrpcShadowResult
                {
                    Status = ConfigurationGrpcShadowStatus.Faulted,
                    TransportResult = ConfigurationTransportResultCode.InvalidRequest
                };
            }

            try
            {
                ConfigurationTransportSnapshot expected =
                    ToTransportSnapshot(authoritative);
                using (var cancellation =
                       new CancellationTokenSource(_timeoutMilliseconds))
                {
                    ConfigurationTransportResult current =
                        _transport.GetAsync(cancellation.Token)
                            .GetAwaiter()
                            .GetResult();
                    if (current.Result == ConfigurationTransportResultCode.Success &&
                        AreEqual(current.Configuration, expected))
                    {
                        return new ConfigurationGrpcShadowResult
                        {
                            Status = ConfigurationGrpcShadowStatus.Matched,
                            Generation = current.Generation,
                            TransportResult = current.Result
                        };
                    }

                    if (current.Result != ConfigurationTransportResultCode.Success &&
                        current.Result != ConfigurationTransportResultCode.Unavailable)
                    {
                        return new ConfigurationGrpcShadowResult
                        {
                            Status = ConfigurationGrpcShadowStatus.Unavailable,
                            Generation = current.Generation,
                            TransportResult = current.Result
                        };
                    }

                    bool seed =
                        current.Result == ConfigurationTransportResultCode.Unavailable;
                    ConfigurationTransportResult updated =
                        _transport.UpdateAsync(expected, cancellation.Token)
                            .GetAwaiter()
                            .GetResult();
                    if (updated.Result != ConfigurationTransportResultCode.Success ||
                        !AreEqual(updated.Configuration, expected))
                    {
                        return new ConfigurationGrpcShadowResult
                        {
                            Status = ConfigurationGrpcShadowStatus.Unavailable,
                            Generation = updated.Generation,
                            TransportResult = updated.Result
                        };
                    }

                    return new ConfigurationGrpcShadowResult
                    {
                        Status = seed
                            ? ConfigurationGrpcShadowStatus.Seeded
                            : ConfigurationGrpcShadowStatus.Resynchronized,
                        Generation = updated.Generation,
                        TransportResult = updated.Result
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new ConfigurationGrpcShadowResult
                {
                    Status = ConfigurationGrpcShadowStatus.TimedOut,
                    TransportResult = ConfigurationTransportResultCode.Unavailable
                };
            }
            catch (Exception)
            {
                return new ConfigurationGrpcShadowResult
                {
                    Status = ConfigurationGrpcShadowStatus.Faulted,
                    TransportResult = ConfigurationTransportResultCode.Unavailable
                };
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _disposableTransport?.Dispose();
        }

        internal static ConfigurationTransportSnapshot ToTransportSnapshot(
            ConfigurationObject configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new ConfigurationTransportSnapshot
            {
                MaxGold = configuration.MaxGold,
                TimeExpBuffUnixTimeMilliseconds =
                    ToUnixTimeMilliseconds(configuration.TimeExpBuff),
                TimeGoldBuffUnixTimeMilliseconds =
                    ToUnixTimeMilliseconds(configuration.TimeGoldBuff)
            };
        }

        internal static bool AreEqual(
            ConfigurationTransportSnapshot left,
            ConfigurationTransportSnapshot right)
        {
            return left != null &&
                   right != null &&
                   left.MaxGold == right.MaxGold &&
                   left.TimeExpBuffUnixTimeMilliseconds ==
                       right.TimeExpBuffUnixTimeMilliseconds &&
                   left.TimeGoldBuffUnixTimeMilliseconds ==
                       right.TimeGoldBuffUnixTimeMilliseconds;
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

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(ConfigurationGrpcShadowMirror));
            }
        }
    }
}
