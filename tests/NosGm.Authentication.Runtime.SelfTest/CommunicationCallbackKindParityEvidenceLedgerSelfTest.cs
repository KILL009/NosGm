using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackKindParityEvidenceLedgerSelfTest
{
    private const string Identity =
        "World:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:1:Sumeria";
    private const string OtherIdentity = "Login";
    private const string Generation1 =
        "11111111-2222-3333-4444-555555555551";
    private const string Generation2 =
        "11111111-2222-3333-4444-555555555552";
    private const string Generation3 =
        "11111111-2222-3333-4444-555555555553";
    private const string Generation4 =
        "11111111-2222-3333-4444-555555555554";
    private const string Generation5 =
        "11111111-2222-3333-4444-555555555555";

    [ModuleInitializer]
    public static void Run()
    {
        VerifySuccessfulQualificationRetention();
        VerifyNegativeEvidenceBreaksQualification();
        VerifyBoundedAndFailClosedBehavior();

        Console.WriteLine(
            "[PASS] Bounded PenaltyRefresh parity qualification ledger self-test");
    }

    private static void VerifySuccessfulQualificationRetention()
    {
        DateTimeOffset start = StartTime();
        var ledger = new CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            capacity: 4);
        CommunicationCallbackKindParityEvidence first =
            Evidence(Identity, Generation1, start);
        CommunicationCallbackKindParityEvidence second =
            Evidence(Identity, Generation2, start.AddMinutes(1));
        CommunicationCallbackKindParityEvidence third =
            Evidence(Identity, Generation3, start.AddMinutes(2));

        AssertEqual(true, ledger.TryAppend(first),
            "The first terminal parity window enters qualification evidence");
        AssertEqual(true, ledger.TryAppend(second),
            "A later generation enters qualification evidence");
        AssertEqual(true, ledger.TryAppend(third),
            "The third distinct generation enters qualification evidence");
        AssertEqual(false, ledger.TryAppend(third),
            "An identical terminal generation retry is idempotent");
        AssertEqual((long)3, ledger.AppendedEvidence,
            "Idempotent retries do not advance the evidence counter");
        AssertEqual(Identity, ledger.ProcessIdentity,
            "The ledger binds to one process identity");

        IReadOnlyList<CommunicationCallbackKindParityEvidence> latest =
            ledger.GetLatest(2);
        AssertEqual(2, latest.Count,
            "The ledger returns a bounded latest evidence suffix");
        AssertEqual(Generation2, latest[0].RuntimeGenerationId,
            "The latest suffix preserves FIFO generation order");
        AssertEqual(Generation3, latest[1].RuntimeGenerationId,
            "The latest suffix ends with the newest generation");

        var gate = new CommunicationCallbackCutoverGate(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        AssertEqual(true, ledger.TryArm(gate),
            "Three retained parity generations arm the PenaltyRefresh gate");
        AssertEqual(CommunicationCallbackCutoverState.Armed, gate.State,
            "Qualification retention does not activate typed effects");
        AssertEqual(
            true,
            gate.ShouldApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "SCS remains authoritative after qualification is armed");
    }

    private static void VerifyNegativeEvidenceBreaksQualification()
    {
        DateTimeOffset start = StartTime();
        var ledger = new CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        ledger.TryAppend(Evidence(Identity, Generation1, start));
        ledger.TryAppend(
            Evidence(
                Identity,
                Generation2,
                start.AddMinutes(1),
                CommunicationCallbackParityVerdict.CountMismatch));
        ledger.TryAppend(
            Evidence(Identity, Generation3, start.AddMinutes(2)));

        var gate = new CommunicationCallbackCutoverGate(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        AssertEqual(false, ledger.TryArm(gate),
            "A terminal mismatch inside the latest evidence window blocks qualification");
        AssertEqual(
            CommunicationCallbackCutoverState.ScsAuthoritative,
            gate.State,
            "Failed qualification leaves SCS authoritative");

        ledger.TryAppend(
            Evidence(Identity, Generation4, start.AddMinutes(3)));
        AssertEqual(false, ledger.TryArm(gate),
            "Two later successes cannot hide the retained mismatch");
        ledger.TryAppend(
            Evidence(Identity, Generation5, start.AddMinutes(4)));
        AssertEqual(true, ledger.TryArm(gate),
            "Three consecutive later parity generations can qualify after a mismatch");
    }

    private static void VerifyBoundedAndFailClosedBehavior()
    {
        DateTimeOffset start = StartTime();
        var ledger = new CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            capacity: 3);
        CommunicationCallbackKindParityEvidence first =
            Evidence(Identity, Generation1, start);
        CommunicationCallbackKindParityEvidence second =
            Evidence(Identity, Generation2, start.AddMinutes(1));
        CommunicationCallbackKindParityEvidence third =
            Evidence(Identity, Generation3, start.AddMinutes(2));
        CommunicationCallbackKindParityEvidence fourth =
            Evidence(Identity, Generation4, start.AddMinutes(3));

        ledger.TryAppend(first);
        ledger.TryAppend(second);
        ledger.TryAppend(third);
        ledger.TryAppend(fourth);

        IReadOnlyList<CommunicationCallbackKindParityEvidence> snapshot =
            ledger.GetSnapshot();
        AssertEqual(3, snapshot.Count,
            "Qualification evidence remains inside its exact capacity");
        AssertEqual(Generation2, snapshot[0].RuntimeGenerationId,
            "Capacity eviction removes the oldest terminal generation");
        AssertEqual(Generation4, snapshot[2].RuntimeGenerationId,
            "Capacity eviction retains the newest terminal generation");
        AssertEqual((long)4, ledger.AppendedEvidence,
            "The cumulative append counter survives FIFO eviction");
        AssertEqual((long)1, ledger.EvictedEvidence,
            "Qualification evidence eviction remains measurable");

        AssertThrows<InvalidOperationException>(
            () => ledger.TryAppend(
                Evidence(
                    Identity,
                    Generation4,
                    start.AddMinutes(4),
                    CommunicationCallbackParityVerdict.OrderMismatch)),
            "One runtime generation cannot produce conflicting terminal evidence");
        AssertThrows<InvalidOperationException>(
            () => ledger.TryAppend(
                Evidence(
                    Identity,
                    Generation5,
                    start.AddMinutes(2).AddSeconds(30))),
            "Terminal evidence cannot arrive out of chronological order");
        AssertThrows<InvalidOperationException>(
            () => ledger.TryAppend(
                Evidence(
                    OtherIdentity,
                    Generation5,
                    start.AddMinutes(5))),
            "Qualification evidence cannot cross process identities");
        AssertThrows<InvalidOperationException>(
            () => ledger.TryAppend(
                new CommunicationCallbackKindParityEvidence(
                    Identity,
                    WireV1.CommunicationCallbackKind.BazaarRefresh,
                    Generation5,
                    CommunicationCallbackParityVerdict.Parity,
                    1,
                    1,
                    start.AddMinutes(5))),
            "Qualification evidence cannot cross callback kinds");
        AssertThrows<InvalidOperationException>(
            () => ledger.TryAppend(
                new CommunicationCallbackKindParityEvidence(
                    Identity,
                    WireV1.CommunicationCallbackKind.PenaltyRefresh,
                    Generation5,
                    CommunicationCallbackParityVerdict.InProgress,
                    0,
                    0,
                    start.AddMinutes(5))),
            "A moving observation window cannot enter terminal qualification evidence");
        AssertThrows<ArgumentOutOfRangeException>(
            () => ledger.GetLatest(0),
            "Latest evidence requests reject zero");
        AssertThrows<ArgumentOutOfRangeException>(
            () => ledger.GetLatest(4),
            "Latest evidence requests cannot exceed ledger capacity");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackKindParityEvidenceLedger(
                WireV1.CommunicationCallbackKind.BazaarRefresh),
            "The first qualification ledger refuses unsupported callback kinds");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackKindParityEvidenceLedger(
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                CommunicationCallbackKindParityEvidenceLedger
                    .MaximumCapacity + 1),
            "Qualification evidence capacity has an absolute ceiling");
    }

    private static CommunicationCallbackKindParityEvidence Evidence(
        string identity,
        string generation,
        DateTimeOffset observedAt,
        CommunicationCallbackParityVerdict verdict =
            CommunicationCallbackParityVerdict.Parity)
    {
        return new CommunicationCallbackKindParityEvidence(
            identity,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            generation,
            verdict,
            1,
            1,
            observedAt);
    }

    private static DateTimeOffset StartTime()
    {
        return new DateTimeOffset(
            2030,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
    }

    private static void AssertThrows<TException>(
        Action action,
        string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine("[PASS] " + name);
            return;
        }

        throw new InvalidOperationException(
            name + ": expected " + typeof(TException).Name + ".");
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
