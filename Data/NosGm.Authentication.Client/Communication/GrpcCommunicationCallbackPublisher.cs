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
using NosGm.Authentication.Client;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public interface ICommunicationCallbackPublisher : IDisposable
    {
        Task<WireV1.PublishCommunicationCallbackResponse> PublishAsync(
            WireV1.PublishCommunicationCallbackRequest publicationTemplate,
            CancellationToken cancellationToken);
    }

    public sealed class GrpcCommunicationCallbackPublisher
        : ICommunicationCallbackPublisher
    {
        private readonly AuthenticationGrpcClientOptions _options;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterCommunicationCallbacks
            .ClusterCommunicationCallbacksClient _client;
        private int _disposed;

        public GrpcCommunicationCallbackPublisher(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            if (options.CallerRole != ClusterNodeRole.Master)
            {
                throw new InvalidOperationException(
                    "The communication callback publisher must use the Master role.");
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
                _client = new WireV1.ClusterCommunicationCallbacks
                    .ClusterCommunicationCallbacksClient(_channel);
            }
            catch
            {
                _httpHandler?.Dispose();
                _clientCertificate.Dispose();
                throw;
            }
        }

        public async Task<WireV1.PublishCommunicationCallbackResponse>
            PublishAsync(
                WireV1.PublishCommunicationCallbackRequest publicationTemplate,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (publicationTemplate == null)
            {
                throw new ArgumentNullException(nameof(publicationTemplate));
            }

            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset deadline = issuedAt.AddMilliseconds(
                _options.DeadlineMilliseconds);
            WireV1.PublishCommunicationCallbackRequest request =
                publicationTemplate.Clone();
            request.Context = CreateRequestContext(issuedAt, deadline);

            WireV1.PublishCommunicationCallbackResponse response =
                await _client.PublishCommunicationCallbackAsync(
                        request,
                        deadline: deadline.UtcDateTime,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            if (response == null)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.DataLoss,
                        "The communication callback runtime returned no publication response."));
            }

            return response;
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
                CallerRole = WireV1.ClusterNodeRole.Master,
                RequestedService = WireV1.ClusterService.Communication,
                CallerInstanceId = _options.CallerInstanceId
            };
        }

        private static X509Certificate2 LoadClientCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The Master communication callback certificate file does not exist.");
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
                    "The Master communication callback certificate has no private key.");
            }

            return certificate;
        }

        private static HttpMessageHandler CreateHttpHandler(
            AuthenticationGrpcClientOptions options,
            X509Certificate2 certificate)
        {
            if (options.WireMode == AuthenticationGrpcWireMode.GrpcWeb)
            {
                var primaryHandler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12
                };
                primaryHandler.ClientCertificates.Add(certificate);
#if NET10_0_OR_GREATER
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

                        using (var certificateCopy =
                               new X509Certificate2(serverCertificate))
                        {
                            return ValidatePinnedServerCertificate(
                                options,
                                certificateCopy,
                                errors);
                        }
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
                    nameof(GrpcCommunicationCallbackPublisher));
            }
        }
    }
}
