using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Communication.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackPublicationOrderSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var time = new PublicationOrderMutableTimeProvider(
            new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero));
        var hub = new CommunicationCallbackHub(
            LoadRuntimeOptions(),
            time);
        Guid reusedEventId = Guid.Parse(
            "11111111-aaaa-bbbb-cccc-222222222222");

        CommunicationCallbackPublishResult expiredGeneration = hub.Publish(
            CreatePenaltyPublish(
                penaltyLogId: 100,
                reusedEventId,
                ttlSeconds: 1));
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            expiredGeneration.Result,
            "Initial callback EventId generation is accepted");

        time.Advance(TimeSpan.FromSeconds(2));
        int maximumPublishedEventIds =
            CommunicationCallbackContractLimits.MaxRetainedEventsPerSubscriber * 4;
        for (int index = 0; index < maximumPublishedEventIds; index++)
        {
            CommunicationCallbackPublishResult filler = hub.Publish(
                CreatePenaltyPublish(
                    penaltyLogId: 1000 + index,
                    Guid.NewGuid(),
                    ttlSeconds: 300));
            if (filler.Result != WireV1.CommunicationResultCode.Success)
            {
                throw new InvalidOperationException(
                    "Callback idempotency capacity filler was rejected at index " +
                    index + ".");
            }
        }

        CommunicationCallbackPublishResult reusedGeneration = hub.Publish(
            CreatePenaltyPublish(
                penaltyLogId: 200,
                reusedEventId,
                ttlSeconds: 300));
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            reusedGeneration.Result,
            "Expired callback EventId may start a new bounded generation");
        AssertEqual(
            true,
            reusedGeneration.Sequence > expiredGeneration.Sequence,
            "Reused callback EventId receives a new accepted sequence");

        CommunicationCallbackPublishResult conflictingGeneration = hub.Publish(
            CreatePenaltyPublish(
                penaltyLogId: 201,
                reusedEventId,
                ttlSeconds: 300));
        AssertEqual(
            WireV1.CommunicationResultCode.Conflict,
            conflictingGeneration.Result,
            "A stale order entry cannot delete the reused EventId generation");
        AssertEqual(
            reusedGeneration.Sequence,
            conflictingGeneration.Sequence,
            "Conflicting reuse reports the surviving generation sequence");

        CommunicationCallbackPublishResult idempotentRetry = hub.Publish(
            CreatePenaltyPublish(
                penaltyLogId: 200,
                reusedEventId,
                ttlSeconds: 300));
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            idempotentRetry.Result,
            "The surviving EventId generation remains idempotent");
        AssertEqual(
            reusedGeneration.Sequence,
            idempotentRetry.Sequence,
            "Idempotent retry preserves the reused generation sequence");

        Console.WriteLine(
            "[PASS] Callback EventId generation-aware capacity trimming self-test");
    }

    private static CommunicationRuntimeOptions LoadRuntimeOptions()
    {
        return CommunicationRuntimeOptions.Load(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        [CommunicationRuntimeOptions
                            .MaximumCallbackSubscribersVariable] = "4",
                        [CommunicationRuntimeOptions.MaximumWorldsVariable] =
                            "4",
                        [CommunicationRuntimeOptions.MaximumAccountsVariable] =
                            "100",
                        [CommunicationRuntimeOptions.SessionTtlVariable] = "300"
                    })
                .Build());
    }

    private static WireV1.PublishCommunicationCallbackRequest
        CreatePenaltyPublish(
            int penaltyLogId,
            Guid eventId,
            uint ttlSeconds)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = ttlSeconds,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
            },
            PenaltyRefresh = new WireV1.PenaltyRefreshCallback
            {
                PenaltyLogId = penaltyLogId
            }
        };
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

    private sealed class PublicationOrderMutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public PublicationOrderMutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
