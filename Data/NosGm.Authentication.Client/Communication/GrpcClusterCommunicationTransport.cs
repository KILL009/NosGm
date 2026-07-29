using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using NosGm.Authentication.Client;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class GrpcClusterCommunicationTransport
        : IClusterCommunicationTransport, IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterCommunication
            .ClusterCommunicationClient _client;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly AuthenticationGrpcClientOptions _options;
        private int _disposed;

        public GrpcClusterCommunicationTransport(
            AuthenticationGrpcClientOptions options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            if (options.CallerRole != ClusterNodeRole.Login &&
                options.CallerRole != ClusterNodeRole.World)
            {
                throw new InvalidOperationException(
                    "The communication gRPC client role must be Login or World.");
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
                _client = new WireV1.ClusterCommunication
                    .ClusterCommunicationClient(_channel);
            }
            catch
            {
                _httpHandler?.Dispose();
                _clientCertificate.Dispose();
                throw;
            }
        }

        public async Task<CommunicationTransportResultCode>
            RegisterAccountLoginAsync(
                long accountId,
                int sessionId,
                string ipAddress,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationMutationResponse response =
                await _client.RegisterAccountLoginAsync(
                        new WireV1.RegisterAccountLoginRequest
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

        public async Task<CommunicationBooleanResult>
            IsAccountSessionRegisteredAsync(
                long accountId,
                int sessionId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationBooleanResponse response =
                await _client.IsAccountSessionRegisteredAsync(
                        CreateAccountSessionRequest(
                            context,
                            accountId,
                            sessionId),
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToBooleanResult(response);
        }

        public async Task<CommunicationBooleanResult> IsLoginPermittedAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationBooleanResponse response =
                await _client.IsLoginPermittedAsync(
                        CreateAccountSessionRequest(
                            context,
                            accountId,
                            sessionId),
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToBooleanResult(response);
        }

        public async Task<CommunicationBooleanResult> IsAccountConnectedAsync(
            long accountId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationBooleanResponse response =
                await _client.IsAccountConnectedAsync(
                        new WireV1.AccountRequest
                        {
                            Context = context,
                            AccountId = accountId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToBooleanResult(response);
        }

        public async Task<CommunicationTransportResultCode> ConnectAccountAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationMutationResponse response =
                await _client.ConnectAccountAsync(
                        new WireV1.ConnectAccountRequest
                        {
                            Context = context,
                            WorldId = worldId.ToString("D"),
                            AccountId = accountId,
                            SessionId = sessionId
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<CommunicationTransportResultCode>
            DisconnectAccountAsync(
                long accountId,
                int sessionId,
                bool preserveSessionRegistration,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationMutationResponse response =
                await _client.DisconnectAccountAsync(
                        new WireV1.DisconnectAccountRequest
                        {
                            Context = context,
                            AccountId = accountId,
                            SessionId = sessionId,
                            PreserveSessionRegistration =
                                preserveSessionRegistration
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<CommunicationTransportResultCode> PulseAccountAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationMutationResponse response =
                await _client.PulseAccountAsync(
                        CreateAccountSessionRequest(
                            context,
                            accountId,
                            sessionId),
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<CommunicationTransportResultCode>
            ConnectCharacterAsync(
                Guid worldId,
                long accountId,
                int sessionId,
                long characterId,
                CancellationToken cancellationToken)
        {
            return await MutateCharacterAsync(
                    true,
                    worldId,
                    accountId,
                    sessionId,
                    characterId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CommunicationTransportResultCode>
            DisconnectCharacterAsync(
                Guid worldId,
                long accountId,
                int sessionId,
                long characterId,
                CancellationToken cancellationToken)
        {
            return await MutateCharacterAsync(
                    false,
                    worldId,
                    accountId,
                    sessionId,
                    characterId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CommunicationWorldRegistrationResult>
            RegisterWorldServerAsync(
                Guid worldId,
                string endpointIp,
                int endpointPort,
                int accountLimit,
                string worldGroup,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.RegisterWorldServerResponse response =
                await _client.RegisterWorldServerAsync(
                        new WireV1.RegisterWorldServerRequest
                        {
                            Context = context,
                            World = new WireV1.WorldServerRegistration
                            {
                                WorldId = worldId.ToString("D"),
                                EndpointIp = endpointIp ?? string.Empty,
                                EndpointPort = ToUnsigned(endpointPort),
                                AccountLimit = ToUnsigned(accountLimit),
                                WorldGroup = worldGroup ?? string.Empty
                            }
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return new CommunicationWorldRegistrationResult
            {
                Result = ToTransportResult(response.Result),
                ChannelId = response.ChannelId
            };
        }

        public async Task<CommunicationTransportResultCode>
            UnregisterWorldServerAsync(
                Guid worldId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.CommunicationMutationResponse response =
                await _client.UnregisterWorldServerAsync(
                        new WireV1.WorldRequest
                        {
                            Context = context,
                            WorldId = worldId.ToString("D")
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
        }

        public async Task<CommunicationWorldListResult> ListWorldServersAsync(
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            WireV1.ListWorldServersResponse response =
                await _client.ListWorldServersAsync(
                        new WireV1.ListWorldServersRequest
                        {
                            Context = context
                        },
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            var worlds = new List<CommunicationWorldSnapshot>(
                response.Worlds.Count);
            foreach (WireV1.WorldChannelSnapshot world in response.Worlds)
            {
                worlds.Add(new CommunicationWorldSnapshot
                {
                    WorldId = ParseWorldId(world.WorldId),
                    EndpointIp = world.EndpointIp,
                    EndpointPort = checked((int)world.EndpointPort),
                    AccountLimit = checked((int)world.AccountLimit),
                    ConnectedAccounts = checked((int)world.ConnectedAccounts),
                    ChannelId = world.ChannelId,
                    WorldGroup = world.WorldGroup
                });
            }

            return new CommunicationWorldListResult
            {
                Result = ToTransportResult(response.Result),
                Worlds = worlds
            };
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

        private async Task<CommunicationTransportResultCode>
            MutateCharacterAsync(
                bool connect,
                Guid worldId,
                long accountId,
                int sessionId,
                long characterId,
                CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            DateTime deadline = CreateDeadline(out WireV1.RequestContext context);
            var request = new WireV1.CharacterWorldRequest
            {
                Context = context,
                WorldId = worldId.ToString("D"),
                AccountId = accountId,
                SessionId = sessionId,
                CharacterId = characterId
            };
            WireV1.CommunicationMutationResponse response = connect
                ? await _client.ConnectCharacterAsync(
                        request,
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false)
                : await _client.DisconnectCharacterAsync(
                        request,
                        deadline: deadline,
                        cancellationToken: cancellationToken)
                    .ResponseAsync
                    .ConfigureAwait(false);
            return ToTransportResult(response.Result);
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
                RequestedService = WireV1.ClusterService.Communication,
                CallerInstanceId = _options.CallerInstanceId
            };
            return deadline.UtcDateTime;
        }

        private static WireV1.AccountSessionRequest CreateAccountSessionRequest(
            WireV1.RequestContext context,
            long accountId,
            int sessionId)
        {
            return new WireV1.AccountSessionRequest
            {
                Context = context,
                AccountId = accountId,
                SessionId = sessionId
            };
        }

        private static CommunicationBooleanResult ToBooleanResult(
            WireV1.CommunicationBooleanResponse response)
        {
            return new CommunicationBooleanResult
            {
                Result = ToTransportResult(response.Result),
                Value = response.Value
            };
        }

        private static CommunicationTransportResultCode ToTransportResult(
            WireV1.CommunicationResultCode result)
        {
            var mapped = (CommunicationTransportResultCode)(int)result;
            return Enum.IsDefined(
                typeof(CommunicationTransportResultCode),
                mapped)
                ? mapped
                : CommunicationTransportResultCode.Unspecified;
        }

        private static Guid ParseWorldId(string value)
        {
            if (!Guid.TryParseExact(value, "D", out Guid worldId) ||
                worldId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The communication service returned an invalid World ID.");
            }

            return worldId;
        }

        private static uint ToUnsigned(int value)
        {
            return value < 0 ? 0U : checked((uint)value);
        }

        private static X509Certificate2 LoadClientCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The communication gRPC client certificate file does not exist.");
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
                    "The communication gRPC client certificate has no private key.");
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
            bool trusted = chain.Build(serverCertificate);
            if (!trusted)
            {
                Console.Error.WriteLine(
                    "[TLS] Communication server certificate chain rejected: " +
                    string.Join(
                        ",",
                        Array.ConvertAll(
                            chain.ChainStatus,
                            status => status.Status.ToString())));
            }
            return trusted;
        }
#endif

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(GrpcClusterCommunicationTransport));
            }
        }
    }
}
