using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Communication.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackHubSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var time = new CallbackMutableTimeProvider(
            new DateTimeOffset(2032, 4, 5, 6, 7, 8, TimeSpan.Zero));
        CommunicationRuntimeOptions options = CommunicationRuntimeOptions.Load(
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
        var hub = new CommunicationCallbackHub(options, time);
        Guid worldA = Guid.Parse(
            "11111111-2222-3333-4444-555555555555");
        Guid worldB = Guid.Parse(
            "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            hub.RegisterWorld(worldA, 1, "Sumeria"),
            "Callback routing registers the first authoritative World");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            hub.RegisterWorld(worldB, 2, "Sumeria"),
            "Callback routing registers the second authoritative World");

        AssertEqual(
            CallbackSubscriptionOpenResult.Success,
            hub.TryOpenSubscription(
                CreateWorldSubscription(worldA, 1, "world-a", 0),
                out CommunicationCallbackSubscription worldLease),
            "A registered World opens one callback subscription");
        AssertEqual(
            CallbackSubscriptionOpenResult.Success,
            hub.TryOpenSubscription(
                CreateLoginSubscription("login-a", 0),
                out CommunicationCallbackSubscription loginLease),
            "Login opens the restricted callback subscription");

        WireV1.PublishCommunicationCallbackRequest penalty =
            CreatePenaltyPublish(17, Guid.NewGuid());
        CommunicationCallbackPublishResult first = hub.Publish(penalty);
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            first.Result,
            "Master callback publication succeeds");
        AssertEqual((ulong)1, first.Sequence, "Callback sequence starts at one");
        AssertEqual(
            (uint)2,
            first.MatchedSubscribers,
            "All-nodes penalty refresh reaches Login and World");
        AssertPendingSequence(worldLease, 1, "World receives the penalty event");
        AssertPendingSequence(loginLease, 1, "Login receives the penalty event");

        WireV1.PublishCommunicationCallbackRequest duplicate = penalty.Clone();
        duplicate.Context = null;
        CommunicationCallbackPublishResult duplicateResult =
            hub.Publish(duplicate);
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            duplicateResult.Result,
            "An identical canonical event is idempotent");
        AssertEqual(
            first.Sequence,
            duplicateResult.Sequence,
            "An idempotent event preserves its accepted sequence");
        AssertEqual(
            false,
            worldLease.PendingEvents.TryRead(out _),
            "An idempotent event is never delivered twice");

        WireV1.PublishCommunicationCallbackRequest conflict =
            CreatePenaltyPublish(18, Guid.Parse(penalty.EventId));
        AssertEqual(
            WireV1.CommunicationResultCode.Conflict,
            hub.Publish(conflict).Result,
            "Reusing an event ID with another payload fails closed");

        CommunicationCallbackPublishResult presence = hub.Publish(
            CreatePresencePublish(
                9001,
                connected: true,
                worldA,
                Guid.NewGuid()));
        AssertEqual(
            (uint)1,
            presence.MatchedSubscribers,
            "Exact-World presence excludes Login and other Worlds");
        AssertPendingSequence(
            worldLease,
            presence.Sequence,
            "The exact target World receives presence");
        AssertEqual(
            false,
            loginLease.PendingEvents.TryRead(out _),
            "Login receives only callback kinds allowed by the contract");

        worldLease.DisposeAsync().AsTask().GetAwaiter().GetResult();
        CommunicationCallbackPublishResult offlineBazaar = hub.Publish(
            CreateBazaarPublish(7001, "Sumeria", Guid.NewGuid()));
        AssertEqual(
            (uint)1,
            offlineBazaar.MatchedSubscribers,
            "Known offline subscriber state retains matching events");
        AssertEqual(
            CallbackSubscriptionOpenResult.Success,
            hub.TryOpenSubscription(
                CreateWorldSubscription(
                    worldA,
                    1,
                    "world-a",
                    presence.Sequence),
                out CommunicationCallbackSubscription replayLease),
            "World reconnects with its durable cursor");
        AssertEqual(
            1,
            replayLease.ReplayEvents.Count,
            "Reconnect replays only newer retained events");
        AssertEqual(
            offlineBazaar.Sequence,
            replayLease.ReplayEvents[0].Sequence,
            "Replay preserves the global accepted sequence");

        AssertEqual(
            CallbackSubscriptionOpenResult.Conflict,
            hub.TryOpenSubscription(
                CreateWorldSubscription(
                    worldA,
                    1,
                    "world-a",
                    offlineBazaar.Sequence),
                out _),
            "A second active stream for one process identity is rejected");
        AssertEqual(
            CallbackSubscriptionOpenResult.InvalidResumeCursor,
            hub.TryOpenSubscription(
                CreateLoginSubscription("unknown-login", 1),
                out _),
            "A new process cannot claim an unknown replay cursor");

        hub.BindCharacter(worldA, 42, 5001, 9001);
        CommunicationCallbackPublishResult staticBonus = hub.Publish(
            CreateStaticBonusPublish(9001, Guid.NewGuid()));
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            staticBonus.Result,
            "Character-targeted callback resolves an attached character");
        AssertEqual(
            (uint)1,
            staticBonus.MatchedSubscribers,
            "Character-targeted callback reaches only its World");
        hub.DisconnectAccount(42, 5001);
        AssertEqual(
            WireV1.CommunicationResultCode.NotFound,
            hub.Publish(
                CreateStaticBonusPublish(9001, Guid.NewGuid())).Result,
            "Disconnected character routes cannot receive callbacks");

        replayLease.DisposeAsync().AsTask().GetAwaiter().GetResult();
        CommunicationCallbackPublishResult expiring = hub.Publish(
            CreateBazaarPublish(
                7002,
                "Sumeria",
                Guid.NewGuid(),
                ttlSeconds: 1));
        time.Advance(TimeSpan.FromSeconds(2));
        AssertEqual(
            CallbackSubscriptionOpenResult.Success,
            hub.TryOpenSubscription(
                CreateWorldSubscription(
                    worldA,
                    1,
                    "world-a",
                    staticBonus.Sequence),
                out CommunicationCallbackSubscription expiredReplayLease),
            "World reconnects after an event TTL expires");
        AssertEqual(
            0,
            expiredReplayLease.ReplayEvents.Count,
            "Expired callbacks are never replayed");
        expiredReplayLease.DisposeAsync().AsTask().GetAwaiter().GetResult();

        AssertEqual(
            CallbackSubscriptionOpenResult.Success,
            hub.TryOpenSubscription(
                CreateWorldSubscription(worldB, 2, "world-b", 0),
                out CommunicationCallbackSubscription overflowLease),
            "Second World opens an isolated bounded queue");
        for (int index = 0;
             index <= CommunicationCallbackContractLimits
                 .MaxPendingEventsPerSubscriber;
             index++)
        {
            CommunicationCallbackPublishResult result = hub.Publish(
                CreateWorldBazaarPublish(
                    worldB,
                    8000 + index,
                    Guid.NewGuid()));
            AssertEqual(
                WireV1.CommunicationResultCode.Success,
                result.Result,
                "Bounded queue load event is accepted");
        }
        AssertEqual(
            CallbackSubscriptionTerminationReason.QueueOverflow,
            overflowLease.TerminationReason,
            "A subscriber that falls behind is terminated instead of growing memory");
        AssertEqual(
            true,
            overflowLease.TerminationToken.IsCancellationRequested,
            "Queue overflow wakes the streaming RPC immediately");

        overflowLease.DisposeAsync().AsTask().GetAwaiter().GetResult();
        loginLease.DisposeAsync().AsTask().GetAwaiter().GetResult();
        Console.WriteLine(
            "[PASS] Communication callback hub replay, idempotency, routing, TTL and bounded overflow self-test");
    }

    private static WireV1.SubscribeCommunicationCallbacksRequest
        CreateWorldSubscription(
            Guid worldId,
            int channelId,
            string instanceId,
            ulong resumeAfter)
    {
        return new WireV1.SubscribeCommunicationCallbacksRequest
        {
            Context = CreateContext(WireV1.ClusterNodeRole.World, instanceId),
            WorldId = worldId.ToString("D"),
            ChannelId = channelId,
            WorldGroup = "Sumeria",
            ResumeAfterSequence = resumeAfter
        };
    }

    private static WireV1.SubscribeCommunicationCallbacksRequest
        CreateLoginSubscription(string instanceId, ulong resumeAfter)
    {
        return new WireV1.SubscribeCommunicationCallbacksRequest
        {
            Context = CreateContext(WireV1.ClusterNodeRole.Login, instanceId),
            ResumeAfterSequence = resumeAfter
        };
    }

    private static WireV1.RequestContext CreateContext(
        WireV1.ClusterNodeRole role,
        string instanceId)
    {
        return new WireV1.RequestContext
        {
            CallerRole = role,
            CallerInstanceId = instanceId
        };
    }

    private static WireV1.PublishCommunicationCallbackRequest
        CreatePenaltyPublish(int penaltyLogId, Guid eventId)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = 30,
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

    private static WireV1.PublishCommunicationCallbackRequest
        CreatePresencePublish(
            long characterId,
            bool connected,
            Guid worldId,
            Guid eventId)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = 30,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.WorldId,
                WorldId = worldId.ToString("D")
            },
            CharacterPresence = new WireV1.CharacterPresenceCallback
            {
                CharacterId = characterId,
                Connected = connected
            }
        };
    }

    private static WireV1.PublishCommunicationCallbackRequest
        CreateBazaarPublish(
            long bazaarItemId,
            string worldGroup,
            Guid eventId,
            uint ttlSeconds = 30)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = ttlSeconds,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.WorldGroup,
                WorldGroup = worldGroup
            },
            BazaarRefresh = new WireV1.BazaarRefreshCallback
            {
                BazaarItemId = bazaarItemId
            }
        };
    }

    private static WireV1.PublishCommunicationCallbackRequest
        CreateWorldBazaarPublish(
            Guid worldId,
            long bazaarItemId,
            Guid eventId)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = 300,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.WorldId,
                WorldId = worldId.ToString("D")
            },
            BazaarRefresh = new WireV1.BazaarRefreshCallback
            {
                BazaarItemId = bazaarItemId
            }
        };
    }

    private static WireV1.PublishCommunicationCallbackRequest
        CreateStaticBonusPublish(long characterId, Guid eventId)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            EventId = eventId.ToString("D"),
            TtlSeconds = 30,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.CharacterId,
                CharacterId = characterId
            },
            StaticBonusRefresh = new WireV1.StaticBonusRefreshCallback
            {
                CharacterId = characterId
            }
        };
    }

    private static void AssertPendingSequence(
        CommunicationCallbackSubscription subscription,
        ulong expectedSequence,
        string name)
    {
        if (!subscription.PendingEvents.TryRead(
                out WireV1.CommunicationCallbackEnvelope envelope))
        {
            throw new InvalidOperationException(name + ": no event was queued.");
        }
        AssertEqual(expectedSequence, envelope.Sequence, name);
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }

        if (!name.StartsWith("Bounded queue load event", StringComparison.Ordinal))
        {
            Console.WriteLine($"[PASS] {name}");
        }
    }

    private sealed class CallbackMutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public CallbackMutableTimeProvider(DateTimeOffset utcNow)
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
