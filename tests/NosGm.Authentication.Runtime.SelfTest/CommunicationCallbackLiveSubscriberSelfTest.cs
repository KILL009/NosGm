using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackLiveSubscriberSelfTest
{
    public static async Task RunLiveAsync()
    {
        string cursorDirectory = Path.Combine(
            Path.GetTempPath(),
            "nosgm-live-callback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cursorDirectory);
        string cursorPath = Path.Combine(cursorDirectory, "login.cursor");
        string subscriberInstanceId =
            "acceptance-login-callback-" + Guid.NewGuid().ToString("N");
        var cursorStore = new SignalingCursorStore();
        var handler = new SignalingHandler();
        CommunicationCallbackSubscriberOptions options =
            CommunicationCallbackSubscriberOptions.Load(
                ClusterNodeRole.Login,
                Guid.Empty,
                0,
                string.Empty,
                name => ReadSubscriberVariable(
                    name,
                    cursorPath,
                    subscriberInstanceId));

        using var subscriber = new GrpcCommunicationCallbackSubscriber(
            options,
            cursorStore,
            handler);
        using var lifetime =
            new CancellationTokenSource(TimeSpan.FromSeconds(45));
        Task subscriberTask = RunSubscriberAsync(
            subscriber,
            lifetime.Token);

        try
        {
            using var publisher = new LiveMasterCallbackPublisher();
            var acceptedPenalties = new Dictionary<ulong, int>();
            WireV1.CommunicationCallbackEnvelope envelope = null;
            for (int attempt = 0; attempt < 30 && envelope == null; attempt++)
            {
                int penaltyLogId = 4242 + attempt;
                WireV1.PublishCommunicationCallbackResponse accepted =
                    await publisher.PublishPenaltyAsync(
                            penaltyLogId,
                            lifetime.Token)
                        .ConfigureAwait(false);
                if (accepted.Result !=
                    WireV1.CommunicationResultCode.Success ||
                    accepted.AcceptedSequence == 0)
                {
                    throw new InvalidOperationException(
                        "Live Master callback publication failed with " +
                        accepted.Result + ".");
                }
                acceptedPenalties[accepted.AcceptedSequence] = penaltyLogId;

                using var deliveryPoll =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        lifetime.Token);
                deliveryPoll.CancelAfter(TimeSpan.FromMilliseconds(250));
                try
                {
                    envelope = await handler.WaitAsync(deliveryPoll.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (!lifetime.IsCancellationRequested)
                {
                    // The stream may still be registering. Publish another
                    // uniquely identifiable event and wait again.
                }
            }

            if (envelope == null ||
                !acceptedPenalties.TryGetValue(
                    envelope.Sequence,
                    out int expectedPenaltyLogId))
            {
                throw new InvalidOperationException(
                    "The live Login callback subscriber did not receive an accepted event in time.");
            }

            ulong savedSequence =
                await cursorStore.WaitForSaveAsync(lifetime.Token)
                    .ConfigureAwait(false);
            AssertEqual(
                expectedPenaltyLogId,
                envelope.PenaltyRefresh.PenaltyLogId,
                "Live Login stream applies the typed penalty callback");
            AssertEqual(
                envelope.Sequence,
                savedSequence,
                "Live callback cursor commits after handler completion");
            AssertEqual(
                1,
                handler.Calls,
                "Live callback envelope is applied exactly once");
            Console.WriteLine(
                "[PASS] Live communication callback server stream over " +
                options.WireMode +
                " with post-application cursor commit");
        }
        finally
        {
            lifetime.Cancel();
            subscriber.Dispose();
            try
            {
                Task completed = await Task.WhenAny(
                        subscriberTask,
                        Task.Delay(TimeSpan.FromSeconds(5)))
                    .ConfigureAwait(false);
                if (completed != subscriberTask)
                {
                    throw new InvalidOperationException(
                        "The live callback subscriber did not stop within five seconds.");
                }

                await subscriberTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected shutdown after the live acceptance event is applied.
            }
            finally
            {
                Directory.Delete(cursorDirectory, true);
            }
        }
    }

    private static async Task RunSubscriberAsync(
        GrpcCommunicationCallbackSubscriber subscriber,
        CancellationToken cancellationToken)
    {
        try
        {
            await subscriber.RunAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Grpc.Core.RpcException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (ObjectDisposedException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static string ReadSubscriberVariable(
        string variableName,
        string cursorPath,
        string subscriberInstanceId)
    {
        return variableName switch
        {
            CommunicationCallbackSubscriberOptions.AddressVariable =>
                ReadRequired("NOSGM_AUTH_GRPC_URL"),
            CommunicationCallbackSubscriberOptions.CertificatePathVariable =>
                ReadRequired("NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH"),
            CommunicationCallbackSubscriberOptions.CertificatePasswordVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PASSWORD") ?? string.Empty,
            CommunicationCallbackSubscriberOptions
                    .TrustedRootCertificatePathVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH"),
            CommunicationCallbackSubscriberOptions.CallerInstanceIdVariable =>
                subscriberInstanceId,
            CommunicationCallbackSubscriberOptions.CursorPathVariable =>
                cursorPath,
            CommunicationCallbackSubscriberOptions.SetupDeadlineVariable =>
                "10000",
            CommunicationCallbackSubscriberOptions.WireModeVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_WIRE_MODE"),
            CommunicationCallbackSubscriberOptions
                    .InitialReconnectDelayVariable => "100",
            CommunicationCallbackSubscriberOptions
                    .MaximumReconnectDelayVariable => "1000",
            _ => null
        };
    }

    private static string ReadRequired(string variableName)
    {
        string value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Live callback acceptance requires " + variableName + ".");
        }
        return value;
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }
        Console.WriteLine($"[PASS] {name}");
    }

    private sealed class SignalingCursorStore
        : ICommunicationCallbackCursorStore
    {
        private readonly TaskCompletionSource<ulong> _saved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ulong _sequence;

        public ulong Load()
        {
            return Volatile.Read(ref _sequence);
        }

        public void Save(ulong sequence)
        {
            Volatile.Write(ref _sequence, sequence);
            _saved.TrySetResult(sequence);
        }

        public async Task<ulong> WaitForSaveAsync(
            CancellationToken cancellationToken)
        {
            return await _saved.Task.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed class SignalingHandler
        : ICommunicationCallbackEnvelopeHandler
    {
        private readonly TaskCompletionSource<
            WireV1.CommunicationCallbackEnvelope> _received =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            _received.TrySetResult(envelope.Clone());
            return Task.CompletedTask;
        }

        public async Task<WireV1.CommunicationCallbackEnvelope> WaitAsync(
            CancellationToken cancellationToken)
        {
            return await _received.Task.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed class LiveMasterCallbackPublisher : IDisposable
    {
        private readonly X509Certificate2 _certificate;
        private readonly HttpMessageHandler _handler;
        private readonly GrpcChannel _channel;
        private readonly WireV1.ClusterCommunicationCallbacks
            .ClusterCommunicationCallbacksClient _client;
        private readonly AuthenticationGrpcWireMode _wireMode;

        public LiveMasterCallbackPublisher()
        {
            _wireMode = ParseWireMode(
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_WIRE_MODE"));
            _certificate = X509CertificateLoader.LoadPkcs12FromFile(
                ReadRequired("NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PATH"),
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PASSWORD") ??
                string.Empty,
                X509KeyStorageFlags.UserKeySet);
            _handler = CreateHandler(_wireMode, _certificate);
            _channel = GrpcChannel.ForAddress(
                ReadRequired("NOSGM_AUTH_GRPC_URL"),
                new GrpcChannelOptions
                {
                    HttpHandler = _handler,
                    MaxReceiveMessageSize =
                        ClusterProtocolLimits.MaxInboundMessageBytes,
                    MaxSendMessageSize =
                        ClusterProtocolLimits.MaxOutboundMessageBytes
                });
            _client = new WireV1.ClusterCommunicationCallbacks
                .ClusterCommunicationCallbacksClient(_channel);
        }

        public async Task<WireV1.PublishCommunicationCallbackResponse>
            PublishPenaltyAsync(
                int penaltyLogId,
                CancellationToken cancellationToken)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset deadline = issuedAt.AddSeconds(10);
            return await _client.PublishCommunicationCallbackAsync(
                    new WireV1.PublishCommunicationCallbackRequest
                    {
                        Context = new WireV1.RequestContext
                        {
                            Version = new WireV1.ProtocolVersion
                            {
                                Major = ClusterContractVersion.CurrentMajor,
                                Minor = ClusterContractVersion.CurrentMinor
                            },
                            RequestId = Guid.NewGuid().ToString("D"),
                            IssuedAtUnixTimeMs =
                                issuedAt.ToUnixTimeMilliseconds(),
                            DeadlineUnixTimeMs =
                                deadline.ToUnixTimeMilliseconds(),
                            CallerRole = WireV1.ClusterNodeRole.Master,
                            RequestedService =
                                WireV1.ClusterService.Communication,
                            CallerInstanceId =
                                "acceptance-master-callback-publisher-1"
                        },
                        EventId = Guid.NewGuid().ToString("D"),
                        TtlSeconds = 30,
                        Target = new WireV1.CommunicationCallbackTarget
                        {
                            Kind = WireV1
                                .CommunicationCallbackTargetKind.AllNodes
                        },
                        PenaltyRefresh = new WireV1.PenaltyRefreshCallback
                        {
                            PenaltyLogId = penaltyLogId
                        }
                    },
                    deadline: deadline.UtcDateTime,
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _handler.Dispose();
            _certificate.Dispose();
        }

        private static AuthenticationGrpcWireMode ParseWireMode(string value)
        {
            return string.Equals(
                value,
                "GRPCWEB",
                StringComparison.OrdinalIgnoreCase)
                ? AuthenticationGrpcWireMode.GrpcWeb
                : AuthenticationGrpcWireMode.Http2;
        }

        private static HttpMessageHandler CreateHandler(
            AuthenticationGrpcWireMode wireMode,
            X509Certificate2 certificate)
        {
            string trustedRootPath = Environment.GetEnvironmentVariable(
                "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH");
            if (wireMode == AuthenticationGrpcWireMode.GrpcWeb)
            {
                var primary = new HttpClientHandler
                {
                    ClientCertificateOptions =
                        ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12
                };
                primary.ClientCertificates.Add(certificate);
                if (!string.IsNullOrEmpty(trustedRootPath))
                {
                    primary.ServerCertificateCustomValidationCallback =
                        (_, serverCertificate, _, errors) =>
                            ValidatePinnedCertificate(
                                trustedRootPath,
                                serverCertificate,
                                errors);
                }
                return new GrpcWebHandler(GrpcWebMode.GrpcWeb, primary);
            }

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
            if (!string.IsNullOrEmpty(trustedRootPath))
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
                        return ValidatePinnedCertificate(
                            trustedRootPath,
                            copy,
                            errors);
                    };
            }
            return handler;
        }

        private static bool ValidatePinnedCertificate(
            string trustedRootPath,
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
                    trustedRootPath);
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(trustedRoot);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            return chain.Build(serverCertificate);
        }
    }
}
