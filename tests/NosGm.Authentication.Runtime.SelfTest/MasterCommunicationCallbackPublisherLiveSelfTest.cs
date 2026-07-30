using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class MasterCommunicationCallbackPublisherLiveSelfTest
{
    [ModuleInitializer]
    public static void Register()
    {
        if (!Environment.GetCommandLineArgs()
                .Contains("--live", StringComparer.Ordinal))
        {
            return;
        }

        var thread = new Thread(
            () => RunLiveAsync().GetAwaiter().GetResult())
        {
            IsBackground = false,
            Name = "NosGM Master callback publisher live acceptance"
        };
        thread.Start();
    }

    private static async Task RunLiveAsync()
    {
        CommunicationCallbackMirrorOptions mirrorOptions =
            CommunicationCallbackMirrorOptions.Load(
                name => name ==
                        CommunicationCallbackMirrorOptions.EnabledVariable
                    ? "true"
                    : null);
        if (!mirrorOptions.Enabled)
        {
            throw new InvalidOperationException(
                "The live Master callback publisher activation probe failed.");
        }

        using var publisher = new GrpcCommunicationCallbackPublisher(
            MasterCommunicationGrpcIdentityOptions.Load(
                ReadPublisherVariable));
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string eventId = Guid.NewGuid().ToString("D");
        var template = new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId,
            TtlSeconds =
                CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllWorlds
            },
            GlobalEvent = new WireV1.GlobalEventCallback
            {
                EventType =
                    WireV1.CommunicationGlobalEventType.InstantBattle,
                Value = 0
            }
        };

        WireV1.PublishCommunicationCallbackResponse first =
            await publisher.PublishAsync(template, timeout.Token)
                .ConfigureAwait(false);
        WireV1.PublishCommunicationCallbackResponse retry =
            await publisher.PublishAsync(template, timeout.Token)
                .ConfigureAwait(false);

        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            first.Result,
            "Reusable Master callback publisher is accepted through mTLS");
        AssertEqual(
            first.AcceptedSequence,
            retry.AcceptedSequence,
            "A retried callback EventId preserves the accepted sequence");
        AssertEqual(
            true,
            first.AcceptedSequence > 0,
            "Master callback publication receives a positive runtime sequence");
        Console.WriteLine(
            "[PASS] Reusable Master callback publisher over the configured wire mode");
    }

    private static string ReadPublisherVariable(string variableName)
    {
        return variableName switch
        {
            MasterCommunicationGrpcIdentityOptions.AddressVariable =>
                ReadRequired("NOSGM_AUTH_GRPC_URL"),
            MasterCommunicationGrpcIdentityOptions.CertificatePathVariable =>
                ReadRequired("NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PATH"),
            MasterCommunicationGrpcIdentityOptions.CertificatePasswordVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PASSWORD") ??
                string.Empty,
            MasterCommunicationGrpcIdentityOptions
                    .TrustedRootCertificatePathVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH"),
            MasterCommunicationGrpcIdentityOptions.CallerInstanceIdVariable =>
                "acceptance-master-callback-publisher-reusable-1",
            MasterCommunicationGrpcIdentityOptions.DeadlineVariable =>
                "10000",
            MasterCommunicationGrpcIdentityOptions.WireModeVariable =>
                Environment.GetEnvironmentVariable(
                    "NOSGM_AUTH_GRPC_WIRE_MODE"),
            _ => null
        };
    }

    private static string ReadRequired(string variableName)
    {
        string value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Live callback publisher acceptance requires " +
                variableName +
                ".");
        }
        return value;
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected +
                "', received '" + actual + "'.");
        }
        Console.WriteLine("[PASS] " + name);
    }
}
