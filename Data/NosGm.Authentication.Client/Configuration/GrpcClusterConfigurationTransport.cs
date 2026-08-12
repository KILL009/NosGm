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
using NosGm.Cluster.Contracts.Configuration.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class GrpcClusterConfigurationTransport
        : IClusterConfigurationTransport,
          IClusterConfigurationUpdateStreamTransport,
          IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterConfiguration
            .ClusterConfigurationClient _client;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly AuthenticationGrpcClientOptions _options;
        private int _disposed;

        public GrpcClusterConfigurationTransport(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            if (options.CallerRole != ClusterNodeRole.World)
            {
                throw new InvalidOperationException(
                    "The Configuration gRPC client role must be World.");
            }

            _clientCertificate = LoadClientCertificate(options);
            try
            {
                _httpHandler = CreateHttpHandler(
                    options,
                    _clientCertificate);
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

        public async Task<ConfigurationTransportResult> GetAsync(
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.GetConfigurationResponse response =
                await _client.GetConfigurationAsync(
                        new WireV1.GetConfigurationRequest
                        {
                            Context = context
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(
                response.Result,
                response.Configuration,
                response.Generation,
                response.RuntimeGenerationId);
        }

        public async Task<ConfigurationTransportResult> UpdateAsync(
            ConfigurationTransportSnapshot configuration,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.UpdateConfigurationResponse response =
                await _client.UpdateConfigurationAsync(
                        new WireV1.UpdateConfigurationRequest
                        {
                            Context = context,
                            Configuration = ToWireSnapshot(configuration)
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(
                response.Result,
                response.Configuration,
                response.Generation,
                response.RuntimeGenerationId);
        }

        public async Task SubscribeUpdatesAsync(
            string runtimeGenerationId,
            ulong resumeAfterGeneration,
            IClusterConfigurationUpdateHandler handler,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!IsCanonicalRuntimeGeneration(runtimeGenerationId))
            {
                throw new ArgumentException(
                    "The Configuration runtime generation must be a canonical GUID.",
                    nameof(runtimeGenerationId));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset setupDeadline =
                issuedAt.AddMilliseconds(_options.DeadlineMilliseconds);
            var request = new WireV1.SubscribeConfigurationUpdatesRequest
            {
                Context = CreateRequestContext(issuedAt, setupDeadline),
                RuntimeGenerationId = runtimeGenerationId,
                ResumeAfterGeneration = resumeAfterGeneration
            };
            using AsyncServerStreamingCall<WireV1.ConfigurationUpdateEnvelope>
                call = _client.SubscribeConfigurationUpdates(
                    request,
                    cancellationToken: cancellationToken);
            while (await call.ResponseStream
                       .MoveNext(cancellationToken)
                       .ConfigureAwait(false))
            {
                WireV1.ConfigurationUpdateEnvelope current =
                    call.ResponseStream.Current;
                await handler.ObserveAsync(
                        ToTransportUpdate(current),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
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
            DateTimeOffset deadline =
                issuedAt.AddMilliseconds(_options.DeadlineMilliseconds);
            context = CreateRequestContext(issuedAt, deadline);
            return deadline.UtcDateTime;
        }

        private WireV1.RequestContext CreateRequestContext(
            DateTimeOffset issuedAt,
            DateTimeOffset deadline)
        {
            return new WireV1.RequestContext
            {
                Version = new WireV1.ProtocolVersion
                {
                    Major = ClusterContractVersion.CurrentMajor,
                    Minor = ClusterContractVersion.CurrentMinor
                },
                RequestId = Guid.NewGuid().ToString("D"),
                IssuedAtUnixTimeMs = issuedAt.ToUnixTimeMilliseconds(),
                DeadlineUnixTimeMs = deadline.ToUnixTimeMilliseconds(),
                CallerRole = (WireV1.ClusterNodeRole)(int)_options.CallerRole,
                RequestedService = WireV1.ClusterService.Configuration,
                CallerInstanceId = _options.CallerInstanceId
            };
        }

        private static ConfigurationTransportResult ToTransportResult(
            WireV1.ConfigurationResultCode result,
            WireV1.ConfigurationSnapshot configuration,
            ulong generation,
            string runtimeGenerationId)
        {
            var mapped = (ConfigurationTransportResultCode)(int)result;
            if (!Enum.IsDefined(typeof(ConfigurationTransportResultCode), mapped))
            {
                mapped = ConfigurationTransportResultCode.Unspecified;
            }

            if (mapped == ConfigurationTransportResultCode.Success &&
                (configuration == null ||
                 ClusterConfigurationContractValidator.ValidateSnapshot(
                     configuration) !=
                     ConfigurationContractValidationError.None ||
                 !IsCanonicalRuntimeGeneration(runtimeGenerationId)))
            {
                throw new InvalidOperationException(
                    "The Configuration gRPC service returned success without a snapshot or valid runtime identity.");
            }

            return new ConfigurationTransportResult
            {
                Result = mapped,
                Configuration = configuration == null
                    ? null
                    : new ConfigurationTransportSnapshot
                    {
                        MaxGold = configuration.MaxGold,
                        TimeExpBuffUnixTimeMilliseconds =
                            configuration.TimeExpBuffUnixTimeMs,
                        TimeGoldBuffUnixTimeMilliseconds =
                            configuration.TimeGoldBuffUnixTimeMs
                    },
                Generation = generation,
                RuntimeGenerationId = runtimeGenerationId
            };
        }

        private static ConfigurationTransportUpdate ToTransportUpdate(
            WireV1.ConfigurationUpdateEnvelope envelope)
        {
            if (envelope == null || envelope.Configuration == null ||
                envelope.Generation == 0 ||
                ClusterConfigurationContractValidator.ValidateSnapshot(
                    envelope.Configuration) !=
                    ConfigurationContractValidationError.None ||
                !IsCanonicalRuntimeGeneration(envelope.RuntimeGenerationId))
            {
                throw new RpcException(
                    new Status(
                        StatusCode.DataLoss,
                        "The Configuration update stream returned a malformed envelope."));
            }

            return new ConfigurationTransportUpdate
            {
                Configuration = new ConfigurationTransportSnapshot
                {
                    MaxGold = envelope.Configuration.MaxGold,
                    TimeExpBuffUnixTimeMilliseconds =
                        envelope.Configuration.TimeExpBuffUnixTimeMs,
                    TimeGoldBuffUnixTimeMilliseconds =
                        envelope.Configuration.TimeGoldBuffUnixTimeMs
                },
                Generation = envelope.Generation,
                RuntimeGenerationId = envelope.RuntimeGenerationId,
                Replayed = envelope.Replayed
            };
        }

        private static bool IsCanonicalRuntimeGeneration(string value)
        {
            return Guid.TryParseExact(value, "D", out Guid parsed) &&
                   string.Equals(
                       value,
                       parsed.ToString("D"),
                       StringComparison.Ordinal);
        }

        private static WireV1.ConfigurationSnapshot ToWireSnapshot(
            ConfigurationTransportSnapshot configuration)
        {
            return new WireV1.ConfigurationSnapshot
            {
                MaxGold = configuration.MaxGold,
                TimeExpBuffUnixTimeMs =
                    configuration.TimeExpBuffUnixTimeMilliseconds,
                TimeGoldBuffUnixTimeMs =
                    configuration.TimeGoldBuffUnixTimeMilliseconds
            };
        }

        private static X509Certificate2 LoadClientCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The Configuration gRPC client certificate file does not exist.");
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
                    "The Configuration gRPC client certificate has no private key.");
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
                SslOptions = new System.Net.Security
                    .SslClientAuthenticationOptions
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
                SslProtocols = SslProtocols.Tls12,
                // WinHttpHandler defaults ReceiveDataTimeout to 30 seconds.
                // Configuration subscriptions are intentionally long-lived and
                // may receive no body data for hours when configuration is stable.
                // Unary RPCs remain bounded by their explicit gRPC deadlines.
                ReceiveDataTimeout = Timeout.InfiniteTimeSpan
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
                Console.Error.WriteLine(
                    "[TLS] Configuration server certificate chain rejected: " +
                    string.Join(
                        ",",
                        Array.ConvertAll(
                            chain.ChainStatus,
                            status => status.Status.ToString())));
                return false;
            }

#if !NET10_0_OR_GREATER
            foreach (X509ChainStatus status in chain.ChainStatus)
            {
                if (status.Status != X509ChainStatusFlags.NoError &&
                    status.Status != X509ChainStatusFlags.UntrustedRoot)
                {
                    Console.Error.WriteLine(
                        "[TLS] Configuration server certificate chain rejected: " +
                        status.Status);
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
                Console.Error.WriteLine(
                    "[TLS] Configuration server certificate root does not match the configured root file.");
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
                    nameof(GrpcClusterConfigurationTransport));
            }
        }
    }
}
