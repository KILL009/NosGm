using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackOverlapDeduplicationSelfTest
{
    private const string Identity =
        "World:bbbbbbbb-cccc-dddd-eeee-ffffffffffff:2:Sumeria";
    private const string RequestId =
        "bbbbbbbb-1111-2222-3333-cccccccccccc";
    private const string Generation1 =
        "22222222-2222-3333-4444-555555555551";
    private const string Generation2 =
        "22222222-2222-3333-4444-555555555552";
    private const string Generation3 =
        "22222222-2222-3333-4444-555555555553";
    private const string Generation4 =
        "22222222-2222-3333-4444-555555555554";

    [ModuleInitializer]
    public static void Run()
    {
        VerifyBoundedOverlapLedger();
        VerifyBothDeliveryOrdersApplyExactlyOnce();
        VerifyReorderedAndRepeatedFingerprints();
        VerifyRollbackConsumesLateLegacyTwin();

        Console.WriteLine(
            "[PASS] PenaltyRefresh overlap deduplication self-test");
    }

    private static void VerifyBoundedOverlapLedger()
    {
        var ledger = new CommunicationCallbackOverlapDeduplicationLedger(
            capacity: 2,
            retention: TimeSpan.FromMinutes(1));
        DateTimeOffset start = new DateTimeOffset(
            2032,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        string first = Fingerprint(1);
        string second = Fingerprint(2);

        ledger.RecordApplied(
            CommunicationCallbackParitySource.LegacyScs,
            first,
            start);
        AssertEqual(true,
            ledger.TryConsumeOpposite(
                CommunicationCallbackParitySource.TypedGrpc,
                first,
                start.AddSeconds(1)),
            "Typed delivery consumes an already-applied SCS twin");
        AssertEqual(0, ledger.PendingCount,
            "Consumed overlap evidence leaves no pending twin");

        ledger.RecordApplied(
            CommunicationCallbackParitySource.TypedGrpc,
            second,
            start.AddSeconds(2));
        AssertEqual(true,
            ledger.TryConsumeOpposite(
                CommunicationCallbackParitySource.LegacyScs,
                second,
                start.AddSeconds(3)),
            "SCS delivery consumes an already-applied typed twin");
        AssertEqual((long)2, ledger.DuplicatesSuppressed,
            "Overlap ledger counts both suppressed transport twins");

        ledger.RecordApplied(
            CommunicationCallbackParitySource.LegacyScs,
            first,
            start.AddSeconds(4));
        AssertEqual(true,
            ledger.HasCapacity(start.AddMinutes(2)),
            "Expired overlap evidence restores bounded capacity");
        AssertEqual((long)1, ledger.Expired,
            "Overlap expiry remains observable");
    }

    private static void VerifyBothDeliveryOrdersApplyExactlyOnce()
    {
        CommunicationCallbackOperatorCutoverCoordinator coordinator =
            ActivatedCoordinator();
        int scsEffects = 0;
        int typedEffects = 0;
        string scsFirst = Fingerprint(10);
        string typedFirst = Fingerprint(11);

        AssertEqual(true,
            coordinator.TryApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                scsFirst,
                () => scsEffects++),
            "SCS may win the dual-delivery race without losing the effect");
        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                scsFirst,
                () => typedEffects++),
            "Typed twin is suppressed after SCS wins the race");

        AssertEqual(true,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                typedFirst,
                () => typedEffects++),
            "Typed gRPC may win the dual-delivery race");
        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                typedFirst,
                () => scsEffects++),
            "Legacy twin is suppressed after typed gRPC wins the race");

        AssertEqual(1, scsEffects,
            "SCS-first callback executes exactly one effect");
        AssertEqual(1, typedEffects,
            "Typed-first callback executes exactly one effect");
        AssertEqual((long)2,
            coordinator.GetStatus().OverlapDuplicatesSuppressed,
            "Coordinator reports both cross-transport duplicates");
    }

    private static void VerifyReorderedAndRepeatedFingerprints()
    {
        CommunicationCallbackOperatorCutoverCoordinator coordinator =
            ActivatedCoordinator();
        int effects = 0;
        string repeated = Fingerprint(20);
        string other = Fingerprint(21);

        coordinator.TryApply(
            CommunicationCallbackParitySource.LegacyScs,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            repeated,
            () => effects++);
        coordinator.TryApply(
            CommunicationCallbackParitySource.LegacyScs,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            other,
            () => effects++);
        coordinator.TryApply(
            CommunicationCallbackParitySource.LegacyScs,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            repeated,
            () => effects++);

        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                repeated,
                () => effects++),
            "First repeated typed twin consumes one SCS occurrence");
        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                repeated,
                () => effects++),
            "Second repeated typed twin consumes the second occurrence");
        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                other,
                () => effects++),
            "Out-of-order typed twin matches by semantic fingerprint");
        AssertEqual(3, effects,
            "Reordered repeated dual delivery still applies three logical effects");
        AssertEqual(0, coordinator.GetStatus().PendingOverlapEffects,
            "All reordered transport twins drain the overlap window");
    }

    private static void VerifyRollbackConsumesLateLegacyTwin()
    {
        CommunicationCallbackOperatorCutoverCoordinator coordinator =
            ActivatedCoordinator();
        int typedEffects = 0;
        int scsEffects = 0;
        string fingerprint = Fingerprint(30);

        AssertEqual(true,
            coordinator.TryApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                fingerprint,
                () => typedEffects++),
            "Typed effect is recorded before stream rollback");
        AssertEqual(true,
            coordinator.ObserveStreamEnded(Generation4),
            "Active stream loss closes typed ingress");
        AssertEqual(false,
            coordinator.TryApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                fingerprint,
                () => scsEffects++),
            "Late SCS twin is suppressed even after authority rollback");
        AssertEqual(1, typedEffects,
            "Pre-rollback typed effect remains applied once");
        AssertEqual(0, scsEffects,
            "Rollback does not duplicate a completed typed effect");

        string newFingerprint = Fingerprint(31);
        AssertEqual(true,
            coordinator.TryApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                newFingerprint,
                () => scsEffects++),
            "A new post-rollback callback applies through SCS");
        AssertEqual(1, scsEffects,
            "SCS handles new callbacks after rollback");
    }

    private static CommunicationCallbackOperatorCutoverCoordinator
        ActivatedCoordinator()
    {
        var coordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        coordinator.Configure(
            Identity,
            Options(),
            effectRoutingEnabled: true);
        coordinator.ObserveQualification(QualifiedLedger());
        coordinator.ObserveRuntimeGeneration(Generation4);
        coordinator.CompleteReplay(Generation4);
        return coordinator;
    }

    private static CommunicationCallbackKindParityEvidenceLedger
        QualifiedLedger()
    {
        DateTimeOffset start = new DateTimeOffset(
            2031,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        var ledger = new CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        ledger.TryAppend(Evidence(Generation1, start));
        ledger.TryAppend(Evidence(Generation2, start.AddMinutes(1)));
        ledger.TryAppend(Evidence(Generation3, start.AddMinutes(2)));
        return ledger;
    }

    private static CommunicationCallbackKindParityEvidence Evidence(
        string generation,
        DateTimeOffset observedAt)
    {
        return new CommunicationCallbackKindParityEvidence(
            Identity,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            generation,
            CommunicationCallbackParityVerdict.Parity,
            1,
            1,
            observedAt);
    }

    private static CommunicationCallbackOperatorCutoverOptions Options()
    {
        return CommunicationCallbackOperatorCutoverOptions.Load(
            name => name ==
                    CommunicationCallbackOperatorCutoverOptions
                        .PenaltyRefreshArmRequestVariable
                ? RequestId
                : "false");
    }

    private static string Fingerprint(int penaltyLogId)
    {
        return CommunicationCallbackSemanticFingerprint
            .ComputePenaltyRefresh(penaltyLogId);
    }

    private static void AssertEqual<T>(
        T expected,
        T actual,
        string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }

        Console.WriteLine("[PASS] " + name);
    }
}
