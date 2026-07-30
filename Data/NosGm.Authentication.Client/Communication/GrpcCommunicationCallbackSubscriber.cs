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
    public interface ICommunicationCallbackEnvelopeHandler
    {
        Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken);
    }

    public interface ICommunicationCallbackSubscriberRunner : IDisposable
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    public sealed class GrpcCommunicationCallbackSubscriber
        : ICommunicationCallbackSubscriberRunner
    {
        private readonly CommunicationCallbackSubscriberOptions _options;
        private readonly CommunicationCallbackProcessor _processor;
        private readonly CommunicationCallbackReplayTracker _replayTracker =
            new CommunicationCallbackReplayTracker();
        private readonly ICommunicationCallbackStreamObservationContext
            _streamObservationContext;
        private readonly X509Certificate2 _clientCertificate;
        private readonly HttpMessageHandler _httpHandler;
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterCommunicationCallbacks
            .ClusterCommunicationCallbacksClient _client;
        private string _shadowWorldGeneration = string.Empty;
        private int _disposed;
        private int _running;

        public GrpcCommunicationCallbackSubscriber(
            CommunicationCallbackSubscriberOptions options,
            ICommunicationCallbackCursorStore cursorStore,
            ICommunicationCallbackEnvelopeHandler handler)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            ICommunicationCallbackEnvelopeHandler envelopeHandler =
                handler ?? throw new ArgumentNullException(nameof(handler));
            _streamObservationContext = envelopeHandler as
                ICommunicationCallbackStreamObservationContext;
            _processor = new CommunicationCallbackProcessor(
                cursorStore ??
                    throw new ArgumentNullException(nameof(cursorStore)),
                envelopeHandler);
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

        public ulong AppliedSequence => _processor.AppliedSequence;

        public bool IsReplayComplete => _replayTracker.IsComplete;

        public CommunicationCallbackReplayEvidence ReplayEvidence =>
            _replayTracker.Evidence;

        public string RuntimeGenerationId =>
            _processor.RuntimeGenerationId;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                throw new InvalidOperationException(
                    "The communication callback subscriber is already running.");
            }

            int reconnectDelay =
                _options.InitialReconnectDelayMilliseconds;
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await RunSingleStreamAsync(cancellationToken)
                            .ConfigureAwait(false);
                        reconnectDelay =
                            _options.InitialReconnectDelayMilliseconds;
                        await Task.Delay(reconnectDelay, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (RpcException exception)
                        when (ShouldReconnect(exception, cancellationToken))
                    {
                        if (_options.CallerRole == ClusterNodeRole.World &&
                            exception.StatusCode ==
                                StatusCode.FailedPrecondition)
                        {
                            _shadowWorldGeneration = string.Empty;
                        }
                        await Task.Delay(reconnectDelay, cancellationToken)
                            .ConfigureAwait(false);
                        reconnectDelay = Math.Min(
                            _options.MaximumReconnectDelayMilliseconds,
                            checked(reconnectDelay * 2));
                    }
                }
            }
            finally
            {
                _streamObservationContext?.EndStream();
                _replayTracker.Reset();
                string registeredGeneration = _shadowWorldGeneration;
                _shadowWorldGeneration = string.Empty;
                if (_options.CallerRole == ClusterNodeRole.World &&
                    !string.IsNullOrEmpty(registeredGeneration))
                {
                    await TryUnregisterShadowWorldAsync(registeredGeneration)
                        .ConfigureAwait(false);
                }
                Volatile.Write(ref _running, 0);
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

        private async Task RunSingleStreamAsync(
            CancellationToken cancellationToken)
        {
            _streamObservationContext?.EndStream();
            _replayTracker.Reset();
            try
            {
                WireV1.GetCommunicationCallbackRuntimeInfoResponse runtimeInfo =
                    await GetRuntimeInfoAsync(cancellationToken)
                        .ConfigureAwait(false);
                _processor.BindRuntimeGeneration(runtimeInfo.RuntimeGenerationId);
                ulong resumeAfterSequence = _processor.AppliedSequence;
                _replayTracker.BeginStream(
                    runtimeInfo.RuntimeGenerationId,
                    resumeAfterSequence);
                _streamObservationContext?.BeginStream(
                    runtimeInfo.RuntimeGenerationId,
                    resumeAfterSequence);

                if (_options.CallerRole == ClusterNodeRole.World &&
                    !string.Equals(
                        _shadowWorldGeneration,
                        runtimeInfo.RuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    await RegisterShadowWorldAsync(
                            runtimeInfo.RuntimeGenerationId,
                            cancellationToken)
                        .ConfigureAwait(false);
                    _shadowWorldGeneration = runtimeInfo.RuntimeGenerationId;
                }

                WireV1.SubscribeCommunicationCallbacksRequest request =
                    CreateSubscribeRequest(
                        runtimeInfo.RuntimeGenerationId,
                        resumeAfterSequence);
                using AsyncServerStreamingCall<
                        WireV1.CommunicationCallbackEnvelope> call =
                    _client.SubscribeCommunicationCallbacks(
                        request,
                        cancellationToken: cancellationToken);

                while (await call.ResponseStream
                           .MoveNext(cancellationToken)
                           .ConfigureAwait(false))
                {
                    await ProcessStreamEnvelopeAsync(
                            call.ResponseStream.Current,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _streamObservationContext?.EndStream();
                _replayTracker.Reset();
            }
        }

        private async Task ProcessStreamEnvelopeAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (envelope != null &&
                envelope.CallbackCase == WireV1
                    .CommunicationCallbackEnvelope.CallbackOneofCase
                    .ReplayComplete)
            {
                CommunicationCallbackReplayEvidence evidence =
                    _replayTracker.Complete(envelope, now);
                _streamObservationContext?.CompleteReplay(evidence);
                return;
            }

            if (_replayTracker.IsComplete)
            {
                _replayTracker.ValidateLiveSequence(envelope?.Sequence ?? 0);
            }
            else
            {
                _replayTracker.ObserveCallbackBeforeBarrier(
                    envelope?.Sequence ?? 0);
            }

            await _processor.ProcessAsync(
                    envelope,
                    now,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<WireV1.GetCommunicationCallbackRuntimeInfoResponse>
            GetRuntimeInfoAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset setupDeadline = issuedAt.AddMilliseconds(
                _options.SetupDeadlineMilliseconds);
            var request =
                new WireV1.GetCommunicationCallbackRuntimeInfoRequest
                {
                    Context = CreateRequestContext(issuedAt, setupDeadline)
                };
            WireV1.GetCommunicationCallbackRuntimeInfoResponse response =
                await _client.GetCommunicationCallbackRuntimeInfoAsync(
                        request,
                        deadline: setupDeadline.UtcDateTime,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            if (response == null ||
                response.Result != WireV1.CommunicationResultCode.Success)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The callback runtime generation is unavailable."));
            }
            if (!IsCanonicalNonEmptyGuid(response.RuntimeGenerationId) ||
                response.StartedAtUnixTimeMs <= 0 ||
                response.CurrentSequence > (ulong)long.MaxValue)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.DataLoss,
                        "The callback runtime returned malformed generation metadata."));
            }

            return response;
        }

        private async Task RegisterShadowWorldAsync(
            string runtimeGenerationId,
            CancellationToken cancellationToken)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset setupDeadline = issuedAt.AddMilliseconds(
                _options.SetupDeadlineMilliseconds);
            WireV1.CommunicationMutationResponse response =
                await _client.RegisterCommunicationCallbackShadowWorldAsync(
                        new WireV1.RegisterCommunicationCallbackShadowWorldRequest
                        {
                            Context = CreateRequestContext(
                                issuedAt,
                                setupDeadline),
                            RuntimeGenerationId = runtimeGenerationId,
                            WorldId = _options.WorldId.ToString("D"),
                            ChannelId = _options.ChannelId,
                            WorldGroup = _options.WorldGroup
                        },
                        deadline: setupDeadline.UtcDateTime,
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            if (response == null ||
                response.Result != WireV1.CommunicationResultCode.Success)
            {
                throw new RpcException(
                    new Status(
                        response?.Result ==
                            WireV1.CommunicationResultCode.Conflict
                            ? StatusCode.AlreadyExists
                            : StatusCode.FailedPrecondition,
                        "The callback-only World shadow route could not be registered."));
            }
        }

        private async Task TryUnregisterShadowWorldAsync(
            string runtimeGenerationId)
        {
            try
            {
                DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
                DateTimeOffset setupDeadline = issuedAt.AddMilliseconds(
                    _options.SetupDeadlineMilliseconds);
                await _client.UnregisterCommunicationCallbackShadowWorldAsync(
                        new WireV1
                            .UnregisterCommunicationCallbackShadowWorldRequest
                        {
                            Context = CreateRequestContext(
                                issuedAt,
                                setupDeadline),
                            RuntimeGenerationId = runtimeGenerationId,
                            WorldId = _options.WorldId.ToString("D")
                        },
                        deadline: setupDeadline.UtcDateTime,
                        cancellationToken: CancellationToken.None)
                    .ResponseAsync.ConfigureAwait(false);
            }
            catch (RpcException exception)
                when (exception.StatusCode == StatusCode.Cancelled ||
                      exception.StatusCode == StatusCode.DeadlineExceeded ||
                      exception.StatusCode == StatusCode.FailedPrecondition ||
                      exception.StatusCode == StatusCode.Unavailable)
            {
                // The route belongs only to the runtime generation that may
                // already be unavailable. Process exit or restart clears it.
            }
        }

        private WireV1.SubscribeCommunicationCallbacksRequest
            CreateSubscribeRequest(
                string runtimeGenerationId,
                ulong resumeAfterSequence)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset setupDeadline = issuedAt.AddMilliseconds(
                _options.SetupDeadlineMilliseconds);
            var request = new WireV1.SubscribeCommunicationCallbacksRequest
            {
                Context = CreateRequestContext(issuedAt, setupDeadline),
                ResumeAfterSequence = resumeAfterSequence,
                RuntimeGenerationId = runtimeGenerationId,
                SupportsReplayCompleteBarrier = true
            };

            if (_options.CallerRole == ClusterNodeRole.World)
            {
                request.WorldId = _options.WorldId.ToString("D");
                request.ChannelId = _options.ChannelId;
                request.WorldGroup = _options.WorldGroup;
            }

            return request;
        }

        private WireV1.RequestContext CreateRequestContext(
            DateTimeOffset issuedAt,
            DateTimeOffset setupDeadline)
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
                DeadlineUnixTimeMs =
                    setupDeadline.ToUnixTimeMilliseconds(),
                CallerRole =
                    (WireV1.ClusterNodeRole)(int)_options.CallerRole,
                RequestedService = WireV1.ClusterService.Communication,
                CallerInstanceId = _options.CallerInstanceId
            };
        }

        private static bool ShouldReconnect(
            RpcException exception,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            switch (exception.StatusCode)
            {
                case StatusCode.Cancelled:
                case StatusCode.Unknown:
                case StatusCode.DeadlineExceeded:
                case StatusCode.ResourceExhausted:
                case StatusCode.Aborted:
                case StatusCode.Internal:
                case StatusCode.Unavailable:
                case StatusCode.FailedPrecondition:
                    return true;
                default:
                    return false;
            }
        }

        private static X509Certificate2 LoadClientCertificate(
            CommunicationCallbackSubscriberOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The communication callback client certificate file does not exist.");
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
                    "The communication callback client certificate has no private key.");
            }

            return certificate;
        }

        private static HttpMessageHandler CreateHttpHandler(
            CommunicationCallbackSubscriberOptions options,
            X509Certificate2 certificate)
        {
            if (options.WireMode == AuthenticationGrpcWireMode.GrpcWeb)
            {
                var primaryHandler = new HttpClientHandler
                {
                    ClientCertificateOptions =
                        ClientCertificateOption.Manual,
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
            CommunicationCallbackSubscriberOptions options,
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

        private static bool IsCanonicalNonEmptyGuid(string value)
        {
            return value != null &&
                   value.Length == 36 &&
                   Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       parsed.ToString("D"),
                       value,
                       StringComparison.Ordinal);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(GrpcCommunicationCallbackSubscriber));
            }
        }
    }
}
