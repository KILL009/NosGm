using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackCutoverGateSelfTest
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

    [ModuleInitializer]
    public static void Run()
    {
        VerifyKindLocalParityEvidence();
        VerifyPenaltyRefreshAuthorityGate();

        Console.WriteLine(
            "[PASS] PenaltyRefresh callback cutover gate self-test");
    }

    private static void VerifyKindLocalParityEvidence()
    {
        CommunicationCallbackReplayEvidence replay =
            CreateReplayEvidence(Generation1, 20, 0);
        string penaltyFingerprint =
            CommunicationCallbackSemanticFingerprint
                .ComputePenaltyRefresh(7);
        string bazaarFingerprint =
            CommunicationCallbackSemanticFingerprint
                .ComputeBazaarRefresh(8);
        string differentBazaarFingerprint =
            CommunicationCallbackSemanticFingerprint
                .ComputeBazaarRefresh(9);

        CommunicationCallbackParityWindow typed =
            CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation1,
                replay,
                Sample(
                    Generation1,
                    21,
                    WireV1.CommunicationCallbackKind.PenaltyRefresh,
                    penaltyFingerprint),
                Sample(
                    Generation1,
                    22,
                    WireV1.CommunicationCallbackKind.BazaarRefresh,
                    bazaarFingerprint));
        CommunicationCallbackParityWindow scs =
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation1,
                replay,
                Sample(
                    Generation1,
                    1,
                    WireV1.CommunicationCallbackKind.PenaltyRefresh,
                    penaltyFingerprint),
                Sample(
                    Generation1,
                    2,
                    WireV1.CommunicationCallbackKind.BazaarRefresh,
                    differentBazaarFingerprint));

        CommunicationCallbackKindParityEvidence penalty =
            CommunicationCallbackKindParityComparator.Compare(
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                typed,
                scs,
                new DateTimeOffset(
                    2030,
                    1,
                    1,
                    0,
                    0,
                    0,
                    TimeSpan.Zero));
        AssertEqual(
            CommunicationCallbackParityVerdict.Parity,
            penalty.Verdict,
            "PenaltyRefresh can qualify independently of an unrelated callback mismatch");
        AssertEqual(
            1,
            penalty.TypedLiveCount,
            "Kind-local parity retains the typed PenaltyRefresh count");
        AssertEqual(
            1,
            penalty.ScsLiveCount,
            "Kind-local parity retains the SCS PenaltyRefresh count");

        CommunicationCallbackKindParityEvidence bazaar =
            CommunicationCallbackKindParityComparator.Compare(
                WireV1.CommunicationCallbackKind.BazaarRefresh,
                typed,
                scs,
                new DateTimeOffset(
                    2030,
                    1,
                    1,
                    0,
                    1,
                    0,
                    TimeSpan.Zero));
        AssertEqual(
            CommunicationCallbackParityVerdict.OrderMismatch,
            bazaar.Verdict,
            "Kind-local evidence preserves a selected callback mismatch");

        CommunicationCallbackKindParityEvidence absent =
            CommunicationCallbackKindParityComparator.Compare(
                WireV1.CommunicationCallbackKind.RelationRefresh,
                typed,
                scs,
                new DateTimeOffset(
                    2030,
                    1,
                    1,
                    0,
                    2,
                    0,
                    TimeSpan.Zero));
        AssertEqual(
            CommunicationCallbackParityVerdict.NoLiveObservations,
            absent.Verdict,
            "An unobserved callback kind cannot qualify for cutover");
    }

    private static void VerifyPenaltyRefreshAuthorityGate()
    {
        var gate = new CommunicationCallbackCutoverGate(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);

        AssertEqual(
            CommunicationCallbackCutoverState.ScsAuthoritative,
            gate.State,
            "The cutover gate starts with SCS authority");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            CommunicationCallbackParitySource.LegacyScs,
            "PenaltyRefresh initially applies only through SCS");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.BazaarRefresh,
            CommunicationCallbackParitySource.LegacyScs,
            "Unselected callback kinds always remain on SCS");

        DateTimeOffset start =
            new DateTimeOffset(
                2030,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);
        CommunicationCallbackKindParityEvidence first =
            Evidence(Identity, Generation1, start);
        CommunicationCallbackKindParityEvidence second =
            Evidence(
                Identity,
                Generation2,
                start.AddMinutes(1));
        CommunicationCallbackKindParityEvidence third =
            Evidence(
                Identity,
                Generation3,
                start.AddMinutes(2));

        AssertEqual(
            false,
            gate.Arm(new[] { first, second }),
            "Fewer than three successful parity windows cannot arm cutover");
        AssertEqual(
            CommunicationCallbackCutoverState.ScsAuthoritative,
            gate.State,
            "Failed qualification leaves SCS authoritative");

        AssertEqual(
            false,
            gate.Arm(new[] { first, second, second }),
            "Repeated evidence from one runtime generation cannot arm cutover");

        AssertEqual(
            true,
            gate.Arm(new[] { first, second, third }),
            "Three ordered successful parity windows arm PenaltyRefresh");
        AssertEqual(
            CommunicationCallbackCutoverState.Armed,
            gate.State,
            "Arming never changes the effect-applying transport");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            CommunicationCallbackParitySource.LegacyScs,
            "An armed gate still applies PenaltyRefresh only through SCS");

        AssertEqual(
            false,
            gate.Activate(OtherIdentity, Generation4),
            "A different process identity cannot activate qualified evidence");
        AssertEqual(
            false,
            gate.Activate(Identity, Generation3),
            "A generation used for qualification cannot also be the activation generation");
        AssertEqual(
            true,
            gate.Activate(Identity, Generation4),
            "A new generation atomically activates typed PenaltyRefresh authority");
        AssertEqual(
            CommunicationCallbackCutoverState.TypedGrpcAuthoritative,
            gate.State,
            "The selected callback kind records typed authority");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            CommunicationCallbackParitySource.TypedGrpc,
            "Activated PenaltyRefresh applies exactly once through typed gRPC");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.BazaarRefresh,
            CommunicationCallbackParitySource.LegacyScs,
            "Activation cannot move another callback kind away from SCS");

        AssertEqual(
            true,
            gate.Rollback(),
            "Rollback is available after typed activation");
        AssertEqual(
            CommunicationCallbackCutoverState.RolledBack,
            gate.State,
            "Rollback enters a terminal SCS-authoritative state");
        AssertExactlyOneAuthority(
            gate,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            CommunicationCallbackParitySource.LegacyScs,
            "Rollback immediately restores PenaltyRefresh to SCS");
        AssertEqual(
            false,
            gate.Activate(Identity, Generation4),
            "A rolled-back gate cannot reactivate without a new process");
        AssertEqual(
            false,
            gate.Arm(new[] { first, second, third }),
            "A rolled-back gate cannot reuse stale parity evidence");

        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackCutoverGate(
                WireV1.CommunicationCallbackKind.BazaarRefresh),
            "The first production gate refuses unsupported callback kinds");
    }

    private static CommunicationCallbackKindParityEvidence Evidence(
        string identity,
        string generation,
        DateTimeOffset observedAt)
    {
        return new CommunicationCallbackKindParityEvidence(
            identity,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            generation,
            CommunicationCallbackParityVerdict.Parity,
            1,
            1,
            observedAt);
    }

    private static CommunicationCallbackParitySample Sample(
        string generation,
        ulong ordinal,
        WireV1.CommunicationCallbackKind kind,
        string fingerprint)
    {
        return new CommunicationCallbackParitySample(
            generation,
            ordinal,
            kind,
            fingerprint);
    }

    private static CommunicationCallbackParityWindow CreateWindow(
        CommunicationCallbackParitySource source,
        string identity,
        string generation,
        CommunicationCallbackReplayEvidence replayEvidence,
        params CommunicationCallbackParitySample[] samples)
    {
        return new CommunicationCallbackParityWindow(
            source,
            identity,
            generation,
            false,
            replayEvidence,
            samples.Length,
            0,
            samples);
    }

    private static CommunicationCallbackReplayEvidence CreateReplayEvidence(
        string generation,
        ulong replayThrough,
        ulong resumeAfter)
    {
        var tracker = new CommunicationCallbackReplayTracker();
        tracker.BeginStream(generation, resumeAfter);
        return tracker.Complete(
            new WireV1.CommunicationCallbackEnvelope
            {
                Sequence = replayThrough,
                ReplayComplete =
                    new WireV1.CommunicationCallbackReplayComplete
                    {
                        RuntimeGenerationId = generation,
                        ReplayThroughSequence = replayThrough,
                        ResumeAfterSequence = resumeAfter,
                        ReplayedEvents = 0
                    }
            },
            DateTimeOffset.UtcNow);
    }

    private static void AssertExactlyOneAuthority(
        CommunicationCallbackCutoverGate gate,
        WireV1.CommunicationCallbackKind kind,
        CommunicationCallbackParitySource expected,
        string name)
    {
        bool scs = gate.ShouldApply(
            CommunicationCallbackParitySource.LegacyScs,
            kind);
        bool typed = gate.ShouldApply(
            CommunicationCallbackParitySource.TypedGrpc,
            kind);
        int applyingSources = (scs ? 1 : 0) + (typed ? 1 : 0);
        CommunicationCallbackParitySource actual = typed
            ? CommunicationCallbackParitySource.TypedGrpc
            : CommunicationCallbackParitySource.LegacyScs;

        AssertEqual(1, applyingSources, name + " has exactly one authority");
        AssertEqual(expected, actual, name);
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
