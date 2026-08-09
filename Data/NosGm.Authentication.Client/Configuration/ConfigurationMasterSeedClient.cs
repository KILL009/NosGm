using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using NosGm.Cluster.Contracts.Configuration.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Configuration
{
    /// <summary>
    /// Minimal Master-role client used only to seed the authoritative
    /// Configuration snapshot before World starts. It intentionally exposes no
    /// subscription API, so only World can consume Configuration updates.
    /// </summary>
    public sealed class ConfigurationMasterSeedClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterConfiguration
            .ClusterConfigurationClient _client;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly AuthenticationGrpcClientOptions _options;
        private int _disposed;

        public ConfigurationMasterSeedClient(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            if (options.CallerRole != ClusterNodeRole.Master)
            {
                throw new InvalidOperationException(
                    "The Configuration seed client must use the Master role.");
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

        public async Task<ConfigurationTransportResult> SeedAsync(
            ConfigurationTransportSnapshot configuration,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset deadline =
                issuedAt.AddMilliseconds(_options.DeadlineMilliseconds);
            var request = new WireV1.UpdateConfigurationRequest
            {
                Context = new WireV1.RequestContext
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
                },
                Configuration = new WireV1.ConfigurationSnapshot
                {
                    MaxGold = configuration.MaxGold,
                    TimeExpBuffUnixTimeMs =
                        configuration.TimeExpBuffUnixTimeMilliseconds,
                    TimeGoldBuffUnixTimeMs =
                        configuration.TimeGoldBuffUnixTimeMilliseconds
                }
            };

            WireV1.UpdateConfigurationResponse response =
                await _client.UpdateConfigurationAsync(
                        request,
                        deadline: deadline.UtcDateTime,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            return ToTransportResult(response);
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

        private static ConfigurationTransportResult ToTransportResult(
            WireV1.UpdateConfigurationResponse response)
        {
            if (response == null)
            {
                throw new InvalidOperationException(
                    "The Configuration seed service returned no response.");
            }

            var result = (ConfigurationTransportResultCode)(int)response.Result;
            if (!Enum.IsDefined(typeof(ConfigurationTransportResultCode), result))
            {
                result = ConfigurationTransportResultCode.Unspecified;
            }

            if (result == ConfigurationTransportResultCode.Success &&
                (response.Configuration == null ||
                 response.Generation == 0 ||
                 ClusterConfigurationContractValidator.ValidateSnapshot(
                     response.Configuration) !=
                     ConfigurationContractValidationError.None ||
                 !IsCanonicalRuntimeGeneration(
                     response.RuntimeGenerationId)))
            {
                throw new InvalidOperationException(
                    "The Configuration seed service returned malformed success data.");
            }

            return new ConfigurationTransportResult
            {
                Result = result,
                Configuration = response.Configuration == null
                    ? null
                    : new ConfigurationTransportSnapshot
                    {
                        MaxGold = response.Configuration.MaxGold,
                        TimeExpBuffUnixTimeMilliseconds =
                            response.Configuration.TimeExpBuffUnixTimeMs,
                        TimeGoldBuffUnixTimeMilliseconds =
                            response.Configuration.TimeGoldBuffUnixTimeMs
                    },
                Generation = response.Generation,
                RuntimeGenerationId = response.RuntimeGenerationId
            };
        }

        private static bool IsCanonicalRuntimeGeneration(string value)
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
                    "The Configuration Master certificate file does not exist.");
            }

            X509KeyStorageFlags keyStorageFlags =
                Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? X509KeyStorageFlags.UserKeySet
                    : X509KeyStorageFlags.EphemeralKeySet;
#if NET10_0_OR_GREATER
            X509Certificate2 certificate =
                X509CertificateLoader.LoadPkcs12FromFile(
                    options.CertificatePath,
                    options.CertificatePassword,
                    keyStorageFlags);
#else
            var certificate = new X509Certificate2(
                options.CertificatePath,
                options.CertificatePassword,
                keyStorageFlags);
#endif
            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    "The Configuration Master certificate has no private key.");
            }

            return certificate;
        }

        private static HttpMessageHandler CreateHttpHandler(
            AuthenticationGrpcClientOptions options,
            X509Certificate2 certificate)
        {
            if (options.WireMode == AuthenticationGrpcWireMode.GrpcWeb)
            {
#if NET10_0_OR_GREATER
                var primaryHandler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12
                };
                primaryHandler.ClientCertificates.Add(certificate);
                if (!string.IsNullOrEmpty(
                        options.TrustedRootCertificatePath))
                {
                    primaryHandler.ServerCertificateCustomValidationCallback =
                        (_, serverCertificate, _, errors) =>
                            ValidatePinnedServerCertificate(
                                options,
                                serverCertificate,
                                errors);
                }
#else
                WinHttpHandler primaryHandler =
                    CreateLegacyWinHttpHandler(options, certificate);
#endif
                return new GrpcWebHandler(
                    GrpcWebMode.GrpcWeb,
                    primaryHandler);
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

                        using var certificateCopy =
                            new X509Certificate2(serverCertificate);
                        return ValidatePinnedServerCertificate(
                            options,
                            certificateCopy,
                            errors);
                    };
            }
            return handler;
#else
            return CreateLegacyWinHttpHandler(options, certificate);
#endif
        }

#if !NET10_0_OR_GREATER
        private static WinHttpHandler CreateLegacyWinHttpHandler(
            AuthenticationGrpcClientOptions options,
            X509Certificate2 certificate)
        {
            var handler = new WinHttpHandler
            {
                SslProtocols = SslProtocols.Tls12
            };
            handler.ClientCertificates.Add(certificate);
            if (!string.IsNullOrEmpty(options.TrustedRootCertificatePath))
            {
                handler.ServerCertificateValidationCallback =
                    (_, serverCertificate, _, errors) =>
                        ValidatePinnedServerCertificate(
                            options,
                            serverCertificate,
                            errors);
            }
            return handler;
        }
#endif

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
#if NET10_0_OR_GREATER
                X509CertificateLoader.LoadCertificateFromFile(
                    options.TrustedRootCertificatePath);
#else
                new X509Certificate2(options.TrustedRootCertificatePath);
#endif
            using var chain = new X509Chain();
#if NET10_0_OR_GREATER
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(trustedRoot);
#else
            chain.ChainPolicy.ExtraStore.Add(trustedRoot);
            chain.ChainPolicy.VerificationFlags =
                X509VerificationFlags.AllowUnknownCertificateAuthority;
#endif
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
#if NET10_0_OR_GREATER
            chain.ChainPolicy.DisableCertificateDownloads = true;
#endif
            bool trusted = chain.Build(serverCertificate);
            if (!trusted || chain.ChainElements.Count == 0)
            {
                return false;
            }

#if !NET10_0_OR_GREATER
            foreach (X509ChainStatus status in chain.ChainStatus)
            {
                if (status.Status != X509ChainStatusFlags.NoError &&
                    status.Status != X509ChainStatusFlags.UntrustedRoot)
                {
                    return false;
                }
            }

            X509Certificate2 observedRoot =
                chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            if (!string.Equals(
                    Convert.ToBase64String(observedRoot.RawData),
                    Convert.ToBase64String(trustedRoot.RawData),
                    StringComparison.Ordinal))
            {
                return false;
            }
#endif
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(ConfigurationMasterSeedClient));
            }
        }
    }
}
