using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackContractSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var worldSubscribe =
            new WireV1.SubscribeCommunicationCallbacksRequest
            {
                Context = CreateContext(WireV1.ClusterNodeRole.World),
                WorldId = "11111111-2222-3333-4444-555555555555",
                ChannelId = 1,
                WorldGroup = "S2-Sumeria",
                ResumeAfterSequence = 41
            };
        worldSubscribe.AcceptedKinds.Add(
            WireV1.CommunicationCallbackKind.KickSession);
        worldSubscribe.AcceptedKinds.Add(
            WireV1.CommunicationCallbackKind.FamilyRefresh);
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidateSubscribe(
                worldSubscribe),
            "World may create a bounded callback subscription");

        worldSubscribe.AcceptedKinds.Add(
            WireV1.CommunicationCallbackKind.KickSession);
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidAcceptedKinds,
            ClusterCommunicationCallbackContractValidator.ValidateSubscribe(
                worldSubscribe),
            "Duplicate callback filters fail closed");
        worldSubscribe.AcceptedKinds.RemoveAt(
            worldSubscribe.AcceptedKinds.Count - 1);

        var loginSubscribe =
            new WireV1.SubscribeCommunicationCallbacksRequest
            {
                Context = CreateContext(WireV1.ClusterNodeRole.Login)
            };
        loginSubscribe.AcceptedKinds.Add(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidateSubscribe(
                loginSubscribe),
            "Login may subscribe to penalty refresh callbacks");
        loginSubscribe.AcceptedKinds.Clear();
        loginSubscribe.AcceptedKinds.Add(
            WireV1.CommunicationCallbackKind.KickSession);
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidAcceptedKinds,
            ClusterCommunicationCallbackContractValidator.ValidateSubscribe(
                loginSubscribe),
            "Login cannot subscribe to World-only callbacks");

        var presence = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.WorldGroup);
        presence.Target.WorldGroup = "S2-Sumeria";
        presence.CharacterPresence = new WireV1.CharacterPresenceCallback
        {
            CharacterId = 10004,
            Connected = true
        };
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                presence),
            "Master may publish typed character presence to a World group");
        presence.Context.CallerRole = WireV1.ClusterNodeRole.World;
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallerRole,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                presence),
            "World cannot impersonate the callback publisher");
        presence.Context.CallerRole = WireV1.ClusterNodeRole.Master;
        presence.Target.Kind =
            WireV1.CommunicationCallbackTargetKind.AllWorlds;
        presence.Target.WorldGroup = string.Empty;
        AssertEqual(
            CommunicationCallbackContractValidationError.TargetCallbackMismatch,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                presence),
            "Character presence cannot escape its World group");

        var kick = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.AllWorlds);
        kick.KickSession = new WireV1.KickSessionCallback
        {
            AccountId = 42
        };
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(kick),
            "Master may publish an account-bound kick");
        kick.KickSession.ClearAccountId();
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallbackPayload,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(kick),
            "A kick without account or session identity fails closed");

        var lifecycle = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.WorldGroup);
        lifecycle.Target.WorldGroup = "S2-Sumeria";
        lifecycle.Lifecycle = new WireV1.LifecycleCallback
        {
            Action = WireV1.CommunicationLifecycleAction.Shutdown,
            DelaySeconds = 5
        };
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallbackPayload,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                lifecycle),
            "Shutdown cannot smuggle a restart delay");
        lifecycle.Lifecycle.DelaySeconds = 0;
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                lifecycle),
            "World-group shutdown has a typed lifecycle event");

        var globalEvent = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.AllWorlds);
        globalEvent.GlobalEvent = new WireV1.GlobalEventCallback
        {
            EventType = WireV1.CommunicationGlobalEventType.OpenWorldBoss,
            Value = 1
        };
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                globalEvent),
            "Global events use an explicit bounded enum");
        globalEvent.GlobalEvent.EventType =
            WireV1.CommunicationGlobalEventType.Unspecified;
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallbackPayload,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                globalEvent),
            "Unspecified global events fail closed");

        var penalty = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.AllNodes);
        penalty.PenaltyRefresh = new WireV1.PenaltyRefreshCallback
        {
            PenaltyLogId = 7
        };
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                penalty),
            "Penalty refresh targets Login and World subscribers");

        var staticBonus = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.CharacterId);
        staticBonus.Target.CharacterId = 10004;
        staticBonus.StaticBonusRefresh =
            new WireV1.StaticBonusRefreshCallback
            {
                CharacterId = 10005
            };
        AssertEqual(
            CommunicationCallbackContractValidationError.TargetCallbackMismatch,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                staticBonus),
            "Character cache refresh cannot target another character");
        staticBonus.StaticBonusRefresh.CharacterId = 10004;
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                staticBonus),
            "Static bonus refresh is character-bound");

        var expired = CreatePublish(
            WireV1.CommunicationCallbackTargetKind.AllWorlds);
        expired.KickSession = new WireV1.KickSessionCallback
        {
            SessionId = 50219
        };
        expired.TtlSeconds =
            CommunicationCallbackContractLimits.MaxEventTtlSeconds + 1;
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidEventTtl,
            ClusterCommunicationCallbackContractValidator.ValidatePublish(
                expired),
            "Callback events cannot outlive the bounded replay window");

        if (CommunicationCallbackContractLimits.MaxPendingEventsPerSubscriber >
            CommunicationCallbackContractLimits.MaxRetainedEventsPerSubscriber)
        {
            throw new InvalidOperationException(
                "Pending callback capacity cannot exceed retained replay capacity.");
        }

        Console.WriteLine(
            "[PASS] Typed communication callback contract self-test");
    }

    private static WireV1.PublishCommunicationCallbackRequest CreatePublish(
        WireV1.CommunicationCallbackTargetKind targetKind)
    {
        return new WireV1.PublishCommunicationCallbackRequest
        {
            Context = CreateContext(WireV1.ClusterNodeRole.Master),
            EventId = Guid.NewGuid().ToString("D"),
            TtlSeconds =
                CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = targetKind
            }
        };
    }

    private static WireV1.RequestContext CreateContext(
        WireV1.ClusterNodeRole role)
    {
        return new WireV1.RequestContext
        {
            Version = new WireV1.ProtocolVersion
            {
                Major = ClusterContractVersion.Current.Major,
                Minor = ClusterContractVersion.Current.Minor
            },
            RequestId = Guid.NewGuid().ToString("D"),
            IssuedAtUnixTimeMs = 1_800_000_000_000,
            DeadlineUnixTimeMs =
                1_800_000_000_000 +
                ClusterProtocolLimits.DefaultDeadlineMilliseconds,
            CallerRole = role,
            RequestedService = WireV1.ClusterService.Communication,
            CallerInstanceId = "callback-contract-self-test"
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
}
