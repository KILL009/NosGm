using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackParityComparatorSelfTest
{
    private const string Generation =
        "11111111-2222-3333-4444-555555555555";
    private const string OtherGeneration =
        "22222222-3333-4444-5555-666666666666";
    private const string Identity =
        "World:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:1:Sumeria";

    [ModuleInitializer]
    public static void Run()
    {
        CommunicationCallbackReplayEvidence replay =
            CreateReplayEvidence(Generation, 10, 0);
        string fingerprint7 =
            CommunicationCallbackSemanticFingerprint
                .ComputePenaltyRefresh(7);
        string fingerprint8 =
            CommunicationCallbackSemanticFingerprint
                .ComputePenaltyRefresh(8);

        CommunicationCallbackParityWindow typed =
            CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation,
                false,
                replay,
                2,
                0,
                Sample(Generation, 11, fingerprint7),
                Sample(Generation, 12, fingerprint8));
        CommunicationCallbackParityWindow scs =
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                replay,
                2,
                0,
                Sample(Generation, 1, fingerprint7),
                Sample(Generation, 2, fingerprint8));

        CommunicationCallbackParityReport parity =
            CommunicationCallbackParityComparator.Compare(typed, scs);
        AssertEqual(
            CommunicationCallbackParityVerdict.Parity,
            parity.Verdict,
            "FIFO-equivalent live SCS and gRPC callbacks reach parity");
        AssertEqual(
            true,
            parity.HasParity,
            "Only a complete ordered comparison reports parity");
        AssertEqual(
            2,
            parity.TypedLiveCount,
            "Parity report retains the typed live sample count");
        AssertEqual(
            2,
            parity.ScsLiveCount,
            "Parity report retains the SCS live sample count");

        CommunicationCallbackParityReport reordered =
            CommunicationCallbackParityComparator.Compare(
                typed,
                CreateWindow(
                    CommunicationCallbackParitySource.LegacyScs,
                    Identity,
                    Generation,
                    false,
                    replay,
                    2,
                    0,
                    Sample(Generation, 1, fingerprint8),
                    Sample(Generation, 2, fingerprint7)));
        AssertEqual(
            CommunicationCallbackParityVerdict.OrderMismatch,
            reordered.Verdict,
            "Reordered semantic payloads fail parity");
        AssertEqual<int?>(
            0,
            reordered.FirstMismatchIndex,
            "Order mismatch exposes the first differing FIFO position");
        AssertEqual(
            (ulong)11,
            reordered.TypedSequence,
            "Order mismatch retains the typed source sequence");
        AssertEqual(
            (ulong)1,
            reordered.ScsOrdinal,
            "Order mismatch retains the SCS local ordinal");

        AssertVerdict(
            CommunicationCallbackParityVerdict.CountMismatch,
            typed,
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                replay,
                1,
                0,
                Sample(Generation, 1, fingerprint7)),
            "Missing SCS callbacks fail parity");
        AssertVerdict(
            CommunicationCallbackParityVerdict.IncompleteEvidence,
            typed,
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                replay,
                2,
                1,
                Sample(Generation, 1, fingerprint7),
                Sample(Generation, 2, fingerprint8)),
            "Any ledger eviction makes parity evidence incomplete");
        AssertVerdict(
            CommunicationCallbackParityVerdict.InProgress,
            CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation,
                true,
                replay,
                2,
                0,
                Sample(Generation, 11, fingerprint7),
                Sample(Generation, 12, fingerprint8)),
            scs,
            "An active observation window cannot claim terminal parity");
        AssertVerdict(
            CommunicationCallbackParityVerdict.ReplayIncomplete,
            CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation,
                false,
                null,
                0,
                0),
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                null,
                0,
                0),
            "A missing replay barrier fails closed");
        AssertVerdict(
            CommunicationCallbackParityVerdict.GenerationMismatch,
            typed,
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                OtherGeneration,
                false,
                CreateReplayEvidence(OtherGeneration, 10, 0),
                1,
                0,
                Sample(OtherGeneration, 1, fingerprint7)),
            "Evidence from different runtime generations never pairs");
        AssertVerdict(
            CommunicationCallbackParityVerdict.ReplayBoundaryMismatch,
            typed,
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                CreateReplayEvidence(Generation, 9, 0),
                2,
                0,
                Sample(Generation, 1, fingerprint7),
                Sample(Generation, 2, fingerprint8)),
            "Different replay boundaries fail parity");
        AssertVerdict(
            CommunicationCallbackParityVerdict.IdentityMismatch,
            typed,
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                "Login",
                Generation,
                false,
                replay,
                2,
                0,
                Sample(Generation, 1, fingerprint7),
                Sample(Generation, 2, fingerprint8)),
            "Evidence from different process identities never pairs");
        AssertVerdict(
            CommunicationCallbackParityVerdict.NoLiveObservations,
            CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation,
                false,
                replay,
                0,
                0),
            CreateWindow(
                CommunicationCallbackParitySource.LegacyScs,
                Identity,
                Generation,
                false,
                replay,
                0,
                0),
            "An empty live window does not claim positive parity");

        AssertThrows<InvalidOperationException>(
            () => CreateWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                Identity,
                Generation,
                false,
                replay,
                2,
                0,
                Sample(Generation, 12, fingerprint7),
                Sample(Generation, 11, fingerprint8)),
            "Non-monotonic source ordinals fail closed");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackParitySample(
                Generation,
                1,
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
                fingerprint7.ToLowerInvariant()),
            "Parity samples reject non-canonical fingerprints");

        Console.WriteLine(
            "[PASS] Bounded SCS-versus-gRPC callback parity comparator self-test");
    }

    private static CommunicationCallbackParitySample Sample(
        string generation,
        ulong ordinal,
        string fingerprint)
    {
        return new CommunicationCallbackParitySample(
            generation,
            ordinal,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            fingerprint);
    }

    private static CommunicationCallbackParityWindow CreateWindow(
        CommunicationCallbackParitySource source,
        string identity,
        string generation,
        bool active,
        CommunicationCallbackReplayEvidence replayEvidence,
        long observedCallbacks,
        long evictedObservations,
        params CommunicationCallbackParitySample[] samples)
    {
        return new CommunicationCallbackParityWindow(
            source,
            identity,
            generation,
            active,
            replayEvidence,
            observedCallbacks,
            evictedObservations,
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

    private static void AssertVerdict(
        CommunicationCallbackParityVerdict expected,
        CommunicationCallbackParityWindow typed,
        CommunicationCallbackParityWindow scs,
        string name)
    {
        AssertEqual(
            expected,
            CommunicationCallbackParityComparator
                .Compare(typed, scs)
                .Verdict,
            name);
    }

    private static void AssertThrows<TException>(Action action, string name)
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

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }

        Console.WriteLine("[PASS] " + name);
    }
}
