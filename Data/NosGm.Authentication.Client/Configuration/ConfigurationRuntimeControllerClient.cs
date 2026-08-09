using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class ConfigurationRuntimeControllerClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterConfiguration
            .ClusterConfigurationClient _client;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly AuthenticationGrpcClientOptions _options;
        private int _disposed;

        public ConfigurationRuntimeControllerClient(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            if (options.CallerRole != ClusterNodeRole.Master)
            {
                throw new InvalidOperationException(
                    "The Configuration runtime controller must use the Master role.");
            }

            _clientCertificate = LoadClientCertificate(options);
            try
            {
                _httpHandler = CreateHttpHandler(options, _clientCertificate);
                _channel = GrpcChannel.ForAddress(
                    options.Address,
                    new GrpcChannelOptions
                    {
                        HttpHandler = _httpHandler,
                        MaxReceiveMessageSize =
                            ClusterProtocolLimits.MaxInboundMessageBytes,
                        MaxSendMessageSize =
                            ClusterProtocolLimits.MaxOutboundMessageBytes
                    });
                _client = new WireV1.ClusterConfiguration
                    .ClusterConfigurationClient(_channel);
            }
            catch
            {
                _httpHandler?.Dispose();
                _clientCertificate.Dispose();
                throw;
            }
        }

        public async Task<WireV1.GetConfigurationRuntimeInfoResponse>
            GetStatusAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(
                out WireV1.RequestContext context);
            WireV1.GetConfigurationRuntimeInfoResponse response =
                await _client.GetConfigurationRuntimeInfoAsync(
                        new WireV1.GetConfigurationRuntimeInfoRequest
                        {
                            Context = context
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            ValidateStatusResponse(response);
            return response;
        }

        public async Task<WireV1.RestartConfigurationRuntimeResponse>
            RestartAsync(
                string expectedRuntimeGenerationId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!IsCanonicalGeneration(expectedRuntimeGenerationId))
            {
                throw new ArgumentException(
                    "The expected Configuration runtime generation must be a canonical non-empty GUID.",
                    nameof(expectedRuntimeGenerationId));
            }

            DateTime deadline = CreateDeadline(
                out WireV1.RequestContext context);
            WireV1.RestartConfigurationRuntimeResponse response =
                await _client.RestartConfigurationRuntimeAsync(
                        new WireV1.RestartConfigurationRuntimeRequest
                        {
                            Context = context,
                            ExpectedRuntimeGenerationId =
                                expectedRuntimeGenerationId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            ValidateRestartResponse(response);
            return response;
        }

        public async Task<WireV1.GetConfigurationRuntimeInfoResponse>
            WaitForSubscriberAsync(
                string runtimeGenerationId,
                TimeSpan timeout,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!IsCanonicalGeneration(runtimeGenerationId))
            {
                throw new ArgumentException(
                    "The Configuration runtime generation must be a canonical non-empty GUID.",
                    nameof(runtimeGenerationId));
            }
            if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    "The Configuration subscriber wait must be between zero and one minute.");
            }

            DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
            WireV1.GetConfigurationRuntimeInfoResponse status = null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                status = await GetStatusAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (status.Result != WireV1.ConfigurationResultCode.Success)
                {
                    return status;
                }
                if (!string.Equals(
                        status.RuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new RpcException(
                        new Status(
                            StatusCode.Aborted,
                            "The Configuration runtime changed while waiting for its World subscriber."));
                }
                if (status.ActiveSubscribers > 0)
                {
                    return status;
                }

                TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }
                await Task.Delay(
                        remaining < TimeSpan.FromMilliseconds(100)
                            ? remaining
                            : TimeSpan.FromMilliseconds(100),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            while (DateTimeOffset.UtcNow < deadline);

            return status;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            _channel.Dispose();
            _httpHandler.Dispose();
            _clientCertificate.Dispose();
        }

        private DateTime CreateDeadline(out WireV1.RequestContext context)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset deadline = issuedAt.AddMilliseconds(
                _options.DeadlineMilliseconds);
            context = new WireV1.RequestContext
            {
                Version = new WireV1.ProtocolVersion
                {
                    Major = ClusterContractVersion.CurrentMajor,
                    Minor = ClusterContractVersion.CurrentMinor
                },
                RequestId = Guid.NewGuid().ToString("D"),
                IssuedAtUnixTimeMs = issuedAt.ToUnixTimeMilliseconds(),
                DeadlineUnixTimeMs = deadline.ToUnixTimeMilliseconds(),
                CallerRole = WireV1.ClusterNodeRole.Master,
                RequestedService = WireV1.ClusterService.Configuration,
                CallerInstanceId = _options.CallerInstanceId
            };
            return deadline.UtcDateTime;
        }

        private static void ValidateStatusResponse(
            WireV1.GetConfigurationRuntimeInfoResponse response)
        {
            if (response == null)
            {
                throw DataLoss("Configuration runtime returned no status.");
            }
            if (response.Result != WireV1.ConfigurationResultCode.Success)
            {
                return;
            }
            if (!IsCanonicalGeneration(response.RuntimeGenerationId) ||
                response.StartedAtUnixTimeMs <= 0 ||
                response.Seeded != (response.ConfigurationGeneration > 0))
            {
                throw DataLoss(
                    "Configuration runtime returned malformed status.");
            }
        }

        private static void ValidateRestartResponse(
            WireV1.RestartConfigurationRuntimeResponse response)
        {
            if (response == null)
            {
                throw DataLoss(
                    "Configuration runtime returned no restart response.");
            }
            if (response.Result != WireV1.ConfigurationResultCode.Success)
            {
                if ((!string.IsNullOrEmpty(response.RuntimeGenerationId) &&
                     !IsCanonicalGeneration(response.RuntimeGenerationId)) ||
                    (!string.IsNullOrEmpty(
                         response.PreviousRuntimeGenerationId) &&
                     !IsCanonicalGeneration(
                         response.PreviousRuntimeGenerationId)))
                {
                    throw DataLoss(
                        "Configuration runtime returned malformed failure status.");
                }
                return;
            }
            if (!response.ControlEnabled ||
                !IsCanonicalGeneration(
                    response.PreviousRuntimeGenerationId) ||
                !IsCanonicalGeneration(response.RuntimeGenerationId) ||
                string.Equals(
                    response.PreviousRuntimeGenerationId,
                    response.RuntimeGenerationId,
                    StringComparison.Ordinal) ||
                response.StartedAtUnixTimeMs <= 0 ||
                response.ConfigurationGeneration == 0)
            {
                throw DataLoss(
                    "Configuration runtime returned malformed restart status.");
            }
        }

        private static RpcException DataLoss(string message)
        {
            return new RpcException(new Status(StatusCode.DataLoss, message));
        }

        private static bool IsCanonicalGeneration(string value)
        {
            return Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       value,
                       parsed.ToString("D"),
                       StringComparison.Ordinal);
        }

        private static X509Certificate2 LoadClientCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The Configuration runtime controller certificate file does not exist.");
            }
            X509KeyStorageFlags flags =
                Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? X509KeyStorageFlags.UserKeySet
                    : X509KeyStorageFlags.EphemeralKeySet;
#if NET10_0_OR_GREATER
            X509Certificate2 certificate =
                X509CertificateLoader.LoadPkcs12FromFile(
                    options.CertificatePath,
                    options.CertificatePassword,
                    flags);
#else
            var certificate = new X509Certificate2(
                options.CertificatePath,
                options.CertificatePassword,
                flags);
#endif
            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    "The Configuration runtime controller certificate has no private key.");
            }
            return certificate;
        }

        private static HttpMessageHandler CreateHttpHandler(
            AuthenticationGrpcClientOptions options,
            X509Certificate2 certificate)
        {
            if (options.WireMode == AuthenticationGrpcWireMode.GrpcWeb)
            {
                var primary = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12
                };
                primary.ClientCertificates.Add(certificate);
#if NET10_0_OR_GREATER
                if (!string.IsNullOrEmpty(options.TrustedRootCertificatePath))
                {
                    primary.ServerCertificateCustomValidationCallback =
                        (_, serverCertificate, _, errors) =>
                            ValidatePinnedServerCertificate(
                                options,
                                serverCertificate,
                                errors);
                }
#endif
                return new GrpcWebHandler(GrpcWebMode.GrpcWeb, primary);
            }

#if NET10_0_OR_GREATER
            var handler = new SocketsHttpHandler
            {
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols =
                        SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificates = new X509CertificateCollection
                    {
                        certificate
                    }
                }
            };
            if (!string.IsNullOrEmpty(options.TrustedRootCertificatePath))
            {
                handler.SslOptions.RemoteCertificateValidationCallback =
                    (_, serverCertificate, _, errors) =>
                    {
                        if (serverCertificate == null)
                        {
                            return false;
                        }
                        using var copy =
                            new X509Certificate2(serverCertificate);
                        return ValidatePinnedServerCertificate(
                            options,
                            copy,
                            errors);
                    };
            }
            return handler;
#else
            var handler = new WinHttpHandler
            {
                SslProtocols = SslProtocols.Tls12
            };
            handler.ClientCertificates.Add(certificate);
            return handler;
#endif
        }

#if NET10_0_OR_GREATER
        private static bool ValidatePinnedServerCertificate(
            AuthenticationGrpcClientOptions options,
            X509Certificate2 serverCertificate,
            SslPolicyErrors errors)
        {
            if (serverCertificate == null ||
                (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) !=
                SslPolicyErrors.None)
            {
                return false;
            }
            using X509Certificate2 trustedRoot =
                X509CertificateLoader.LoadCertificateFromFile(
                    options.TrustedRootCertificatePath);
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(trustedRoot);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            return chain.Build(serverCertificate);
        }
#endif

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(ConfigurationRuntimeControllerClient));
            }
        }
    }
}
