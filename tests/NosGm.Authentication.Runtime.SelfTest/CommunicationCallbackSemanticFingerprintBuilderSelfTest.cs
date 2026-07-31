using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackSemanticFingerprintBuilderSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                CharacterPresence = new WireV1.CharacterPresenceCallback
                {
                    CharacterId = 9001,
                    Connected = true
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeCharacterPresence(9001, true),
            "Character-presence legacy arguments match typed payload hashing");

        var kick = new WireV1.KickSessionCallback
        {
            AccountId = 42,
            SessionId = 5001
        };
        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                KickSession = kick
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeKickSession(42, 5001),
            "Kick-session legacy arguments match typed payload hashing");
        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                KickSession = new WireV1.KickSessionCallback
                {
                    SessionId = 5002
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeKickSession(null, 5002),
            "Optional kick identity preserves Protobuf field presence");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                Lifecycle = new WireV1.LifecycleCallback
                {
                    Action = WireV1.CommunicationLifecycleAction.Restart,
                    DelaySeconds = 5
                }
            },
            CommunicationCallbackSemanticFingerprint.ComputeLifecycle(
                WireV1.CommunicationLifecycleAction.Restart,
                5),
            "Restart legacy arguments match typed payload hashing");
        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                Lifecycle = new WireV1.LifecycleCallback
                {
                    Action = WireV1.CommunicationLifecycleAction.Shutdown,
                    DelaySeconds = 0
                }
            },
            CommunicationCallbackSemanticFingerprint.ComputeLifecycle(
                WireV1.CommunicationLifecycleAction.Shutdown,
                0),
            "Shutdown legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                GlobalEvent = new WireV1.GlobalEventCallback
                {
                    EventType =
                        WireV1.CommunicationGlobalEventType.OpenWorldBoss,
                    Value = 3
                }
            },
            CommunicationCallbackSemanticFingerprint.ComputeGlobalEvent(
                WireV1.CommunicationGlobalEventType.OpenWorldBoss,
                3),
            "Global-event legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                BazaarRefresh = new WireV1.BazaarRefreshCallback
                {
                    BazaarItemId = 7001
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeBazaarRefresh(7001),
            "Bazaar legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                FamilyRefresh = new WireV1.FamilyRefreshCallback
                {
                    FamilyId = 8001,
                    ChangeFaction = true
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeFamilyRefresh(8001, true),
            "Family legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                PenaltyRefresh = new WireV1.PenaltyRefreshCallback
                {
                    PenaltyLogId = 17
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputePenaltyRefresh(17),
            "Penalty legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                RelationRefresh = new WireV1.RelationRefreshCallback
                {
                    RelationId = 8101
                }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeRelationRefresh(8101),
            "Relation legacy arguments match typed payload hashing");

        AssertFingerprint(
            new WireV1.CommunicationCallbackEnvelope
            {
                StaticBonusRefresh =
                    new WireV1.StaticBonusRefreshCallback
                    {
                        CharacterId = 9001
                    }
            },
            CommunicationCallbackSemanticFingerprint
                .ComputeStaticBonusRefresh(9001),
            "Static-bonus legacy arguments match typed payload hashing");

        Console.WriteLine(
            "[PASS] Legacy and typed callback semantic fingerprint builders self-test");
    }

    private static void AssertFingerprint(
        WireV1.CommunicationCallbackEnvelope envelope,
        string legacyFingerprint,
        string name)
    {
        string typedFingerprint =
            CommunicationCallbackSemanticFingerprint.Compute(envelope);
        if (!string.Equals(
                typedFingerprint,
                legacyFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                name + ": typed and legacy fingerprints differ.");
        }
        Console.WriteLine("[PASS] " + name);
    }
}
