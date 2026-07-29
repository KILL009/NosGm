using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client
{
    public sealed class GrpcGameforgeAuthenticationTransport
        : IGameforgeAuthenticationTransport, IDisposable
    {
        private readonly AuthenticationGrpcClientOptions _options;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly GrpcChannel _channel;
        private readonly WireV1.GameforgeAuthentication
            .GameforgeAuthenticationClient _client;
        private int _disposed;

        public GrpcGameforgeAuthenticationTransport(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _clientCertificate = LoadClientCertificate(options);
            try
            {
                _httpHandler = CreateHttpHandler(_clientCertificate);
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
                _client = new WireV1.GameforgeAuthentication
                    .GameforgeAuthenticationClient(_channel);
            }
            catch
            {
                _httpHandler?.Dispose();
                _clientCertificate.Dispose();
                throw;
            }
        }

        public async Task<AuthenticationTransportResultCode>
            IssueAuthTicketAsync(
                string accountName,
                string authorizationCode,
                string installationId,
                uint countryId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.IssueAuthTicketResponse response =
                await _client.IssueAuthTicketAsync(
                        new WireV1.IssueAuthTicketRequest
                        {
                            Context = context,
                            AccountName = accountName ?? string.Empty,
                            AuthorizationCode =
                                authorizationCode ?? string.Empty,
                            InstallationId = installationId ?? string.Empty,
                            CountryId = countryId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<AuthenticationTicketConsumptionResult>
            ConsumeAuthTicketAsync(
                string authorizationCode,
                string installationId,
                uint countryId,
                int proposedSessionId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.ConsumeAuthTicketResponse response =
                await _client.ConsumeAuthTicketAsync(
                        new WireV1.ConsumeAuthTicketRequest
                        {
                            Context = context,
                            AuthorizationCode =
                                authorizationCode ?? string.Empty,
                            InstallationId = installationId ?? string.Empty,
                            CountryId = countryId,
                            ProposedSessionId = proposedSessionId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return new AuthenticationTicketConsumptionResult
            {
                Result = ToTransportResult(response.Result),
                AccountName = response.AccountName,
                ConsumptionNumber = response.ConsumptionNumber,
                SessionId = response.SessionId
            };
        }

        public async Task<AuthenticationTransportResultCode>
            IssueWorldPermitAsync(
                long accountId,
                int sessionId,
                string ipAddress,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.IssueWorldPermitResponse response =
                await _client.IssueWorldPermitAsync(
                        new WireV1.IssueWorldPermitRequest
                        {
                            Context = context,
                            AccountId = accountId,
                            SessionId = sessionId,
                            IpAddress = ipAddress ?? string.Empty
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<AuthenticationTransportResultCode>
            ConsumeWorldPermitAsync(
                long accountId,
                int sessionId,
                string ipAddress,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.ConsumeWorldPermitResponse response =
                await _client.ConsumeWorldPermitAsync(
                        new WireV1.ConsumeWorldPermitRequest
                        {
                            Context = context,
                            AccountId = accountId,
                            SessionId = sessionId,
                            IpAddress = ipAddress ?? string.Empty
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<AuthenticationTransportResultCode>
            RevokeWorldPermitAsync(
                long accountId,
                int sessionId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.RevokeWorldPermitResponse response =
                await _client.RevokeWorldPermitAsync(
                        new WireV1.RevokeWorldPermitRequest
                        {
                            Context = context,
                            AccountId = accountId,
                            SessionId = sessionId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
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
                CallerRole = (WireV1.ClusterNodeRole)(int)_options.CallerRole,
                RequestedService = WireV1.ClusterService.Authentication,
                CallerInstanceId = _options.CallerInstanceId
            };
            return deadline.UtcDateTime;
        }

        private static AuthenticationTransportResultCode ToTransportResult(
            WireV1.AuthenticationResultCode result)
        {
            var mapped = (AuthenticationTransportResultCode)(int)result;
            return Enum.IsDefined(
                typeof(AuthenticationTransportResultCode),
                mapped)
                ? mapped
                : AuthenticationTransportResultCode.Unspecified;
        }

        private static X509Certificate2 LoadClientCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The authentication gRPC client certificate file does not exist.");
            }

#if NET10_0_OR_GREATER
            X509Certificate2 certificate =
                X509CertificateLoader.LoadPkcs12FromFile(
                    options.CertificatePath,
                    options.CertificatePassword,
                    X509KeyStorageFlags.EphemeralKeySet);
#else
            var certificate = new X509Certificate2(
                options.CertificatePath,
                options.CertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
#endif
            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    "The authentication gRPC client certificate has no private key.");
            }

            return certificate;
        }

        private static HttpMessageHandler CreateHttpHandler(
            X509Certificate2 certificate)
        {
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

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(GrpcGameforgeAuthenticationTransport));
            }
        }
    }
}
