using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Communication
{
    public sealed class GrpcCommunicationCallbackSubscriber : IDisposable
    {
        private const int MaximumRememberedEventIds = 4096;
        private readonly CommunicationCallbackSubscriptionOptions _options;
        private readonly ICommunicationCallbackApplier _applier;
        private readonly X509Certificate2 _certificate;
        private readonly HttpMessageHandler _handler;
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterCommunicationCallbacks.ClusterCommunicationCallbacksClient _client;
        private readonly object _stateLock = new object();
        private readonly HashSet<string> _eventIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _eventIdOrder = new Queue<string>();
        private long _resumeAfterSequence;
        private int _disposed;

        public GrpcCommunicationCallbackSubscriber(
            CommunicationCallbackSubscriptionOptions options,
            ICommunicationCallbackApplier applier)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _applier = applier ?? throw new ArgumentNullException(nameof(applier));
            _certificate = LoadCertificate(options.Transport);
            try
            {
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(_certificate);
                _handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, httpClientHandler);
                _channel = GrpcChannel.ForAddress(
                    options.Transport.Address,
                    new GrpcChannelOptions
                    {
                        HttpHandler = _handler,
                        MaxReceiveMessageSize = ClusterProtocolLimits.MaxInboundMessageBytes,
                        MaxSendMessageSize = ClusterProtocolLimits.MaxOutboundMessageBytes
                    });
                _client = new WireV1.ClusterCommunicationCallbacks
                    .ClusterCommunicationCallbacksClient(_channel);
            }
            catch
            {
                _handler?.Dispose();
                _certificate.Dispose();
                throw;
            }
        }

        public ulong ResumeAfterSequence
        {
            get
            {
                lock (_stateLock)
                {
                    return unchecked((ulong)_resumeAfterSequence);
                }
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            int failedAttempts = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RunConnectedAsync(cancellationToken).ConfigureAwait(false);
                    failedAttempts = 0;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (RpcException exception) when (IsReconnectable(exception.StatusCode))
                {
                    failedAttempts++;
                }

                int delayMilliseconds = Math.Min(30000, 250 * (1 << Math.Min(failedAttempts, 7)));
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task RunConnectedAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            WireV1.SubscribeCommunicationCallbacksRequest request = CreateRequest();
            using (AsyncServerStreamingCall<WireV1.CommunicationCallbackEnvelope> call =
                   _client.SubscribeCommunicationCallbacks(
                       request,
                       cancellationToken: cancellationToken))
            {
                while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    await ApplyEnvelopeAsync(call.ResponseStream.Current, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _channel.Dispose();
            _handler.Dispose();
            _certificate.Dispose();
        }

        private WireV1.SubscribeCommunicationCallbacksRequest CreateRequest()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var request = new WireV1.SubscribeCommunicationCallbacksRequest
            {
                Context = new WireV1.RequestContext
                {
                    Version = new WireV1.ProtocolVersion { Major = 1, Minor = 0 },
                    RequestId = Guid.NewGuid().ToString("D"),
                    IssuedAtUnixTimeMs = now,
                    DeadlineUnixTimeMs = now + _options.Transport.DeadlineMilliseconds,
                    CallerRole = ToWireRole(_options.Transport.CallerRole),
                    RequestedService = WireV1.ClusterService.CommunicationCallback,
                    CallerInstanceId = _options.Transport.CallerInstanceId
                },
                WorldId = _options.WorldId,
                ChannelId = _options.ChannelId,
                WorldGroup = _options.WorldGroup,
                ResumeAfterSequence = ResumeAfterSequence
            };
            request.AcceptedKinds.Add(_options.AcceptedKinds);
            return request;
        }

        private async Task ApplyEnvelopeAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null || envelope.Sequence == 0 ||
                !Guid.TryParse(envelope.EventId, out _) ||
                envelope.ExpiresAtUnixTimeMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                return;
            }

            bool duplicate;
            lock (_stateLock)
            {
                if (envelope.Sequence <= unchecked((ulong)_resumeAfterSequence))
                {
                    return;
                }
                duplicate = _eventIds.Contains(envelope.EventId);
            }

            if (!duplicate)
            {
                await _applier.ApplyAsync(envelope, cancellationToken).ConfigureAwait(false);
            }

            lock (_stateLock)
            {
                if (!duplicate && _eventIds.Add(envelope.EventId))
                {
                    _eventIdOrder.Enqueue(envelope.EventId);
                    while (_eventIdOrder.Count > MaximumRememberedEventIds)
                    {
                        _eventIds.Remove(_eventIdOrder.Dequeue());
                    }
                }
                if (envelope.Sequence > unchecked((ulong)_resumeAfterSequence))
                {
                    _resumeAfterSequence = checked((long)envelope.Sequence);
                }
            }
        }

        private static bool IsReconnectable(StatusCode statusCode)
        {
            return statusCode == StatusCode.Unavailable ||
                   statusCode == StatusCode.Cancelled ||
                   statusCode == StatusCode.DeadlineExceeded ||
                   statusCode == StatusCode.ResourceExhausted;
        }

        private static WireV1.ClusterNodeRole ToWireRole(ClusterNodeRole role)
        {
            return role == ClusterNodeRole.Login
                ? WireV1.ClusterNodeRole.Login
                : WireV1.ClusterNodeRole.World;
        }

        private static X509Certificate2 LoadCertificate(
            AuthenticationGrpcClientOptions options)
        {
            if (!System.IO.File.Exists(options.CertificatePath))
            {
                throw new InvalidOperationException(
                    "The callback subscriber certificate file does not exist.");
            }
#if NET10_0_OR_GREATER
            X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(
                options.CertificatePath,
                options.CertificatePassword,
                X509KeyStorageFlags.UserKeySet);
#else
            X509Certificate2 certificate = new X509Certificate2(
                options.CertificatePath,
                options.CertificatePassword,
                X509KeyStorageFlags.UserKeySet);
#endif
            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    "The callback subscriber certificate has no private key.");
            }
            return certificate;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(GrpcCommunicationCallbackSubscriber));
            }
        }
    }
}
