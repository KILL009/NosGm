using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationPenaltyRefreshLiveProbeSelfTest
{
    private const string ProbeArgument = "--live-penalty-refresh-probe";
    private const int ObservationOnlyPenaltyLogId = int.MaxValue;

    [ModuleInitializer]
    public static void Register()
    {
        if (!Environment.GetCommandLineArgs()
                .Contains(ProbeArgument, StringComparer.Ordinal))
        {
            return;
        }

        var thread = new Thread(
            () => RunLiveAsync().GetAwaiter().GetResult())
        {
            IsBackground = false,
            Name = "NosGM PenaltyRefresh callback observation probe"
        };
        thread.Start();
    }

    private static async Task RunLiveAsync()
    {
        // This probe publishes directly to the typed callback runtime while the
        // local stack is in shadow mode. APPLY remains disabled in Login/World,
        // so the payload is observed and durably cursor-committed without
        // invoking PenaltyLogRefresh or changing gameplay/database state.
        using var publisher = new GrpcCommunicationCallbackPublisher(
            MasterCommunicationGrpcIdentityOptions.Load());
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(30));

        string eventId = Guid.NewGuid().ToString("D");
        var request = new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId,
            TtlSeconds =
                CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
            },
            PenaltyRefresh = new WireV1.PenaltyRefreshCallback
            {
                PenaltyLogId = ObservationOnlyPenaltyLogId
            }
        };

        WireV1.PublishCommunicationCallbackResponse response =
            await publisher.PublishAsync(request, timeout.Token)
                .ConfigureAwait(false);
        if (response.Result != WireV1.CommunicationResultCode.Success)
        {
            throw new InvalidOperationException(
                "PenaltyRefresh probe publication failed with " +
                response.Result + ".");
        }
        if (response.AcceptedSequence == 0)
        {
            throw new InvalidOperationException(
                "PenaltyRefresh probe publication returned sequence zero.");
        }
        if (response.MatchedSubscribers < 2)
        {
            throw new InvalidOperationException(
                "PenaltyRefresh probe expected Login and World subscribers; matched " +
                response.MatchedSubscribers + ".");
        }

        Console.WriteLine(
            "[CALLBACK_PENALTY_PROBE] AcceptedSequence=" +
            response.AcceptedSequence +
            " MatchedSubscribers=" + response.MatchedSubscribers +
            " PenaltyLogId=" + ObservationOnlyPenaltyLogId);
        Console.WriteLine(
            "[PASS] PenaltyRefresh typed observation probe published to Login and World routes");
    }
}
