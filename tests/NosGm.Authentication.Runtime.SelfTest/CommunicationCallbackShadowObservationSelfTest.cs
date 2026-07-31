using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackShadowObservationSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        const string generation =
            "11111111-2222-3333-4444-555555555555";
        var handler = new CommunicationCallbackShadowEnvelopeHandler(2);
        var tracker = new CommunicationCallbackReplayTracker();

        handler.BeginStream(generation, 0);
        tracker.BeginStream(generation, 0);
        AssertEqual(
            true,
            handler.IsStreamActive,
            "Typed observation stream reports its active state");
        AssertThrows<InvalidOperationException>(
            () => handler.BeginStream(generation, 0),
            "An active typed observation stream cannot be replaced silently");

        WireV1.CommunicationCallbackEnvelope replay =
            CreatePenaltyEnvelope(1, 7);
        handler.ApplyAsync(replay, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        tracker.ObserveCallbackBeforeBarrier(replay.Sequence);

        IReadOnlyList<CommunicationCallbackShadowObservation> replaySnapshot =
            handler.GetObservationSnapshot();
        AssertEqual(1, replaySnapshot.Count, "Replay observation is retained");
        AssertEqual(
            CommunicationCallbackObservationPhase.Replay,
            replaySnapshot[0].Phase,
            "Observation before the barrier is classified as replay");
        AssertEqual(
            generation,
            replaySnapshot[0].RuntimeGenerationId,
            "Replay observation preserves the runtime generation");
        AssertEqual(
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            replaySnapshot[0].Kind,
            "Observation records the typed callback kind");
        AssertEqual(
            64,
            replaySnapshot[0].SemanticFingerprint.Length,
            "Observation fingerprint is a SHA-256 hexadecimal digest");

        CommunicationCallbackReplayEvidence evidence = tracker.Complete(
            CreateBarrier(generation, 1, 0, 1),
            DateTimeOffset.UtcNow);
        handler.CompleteReplay(evidence);

        WireV1.CommunicationCallbackEnvelope samePayload =
            CreatePenaltyEnvelope(2, 7);
        samePayload.IssuedAtUnixTimeMs += 5000;
        samePayload.ExpiresAtUnixTimeMs += 5000;
        handler.ApplyAsync(samePayload, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        IReadOnlyList<CommunicationCallbackShadowObservation> twoObservations =
            handler.GetObservationSnapshot();
        AssertEqual(2, twoObservations.Count, "Ledger fills to its exact capacity");
        AssertEqual(
            CommunicationCallbackObservationPhase.Live,
            twoObservations[1].Phase,
            "Observation after the barrier is classified as live");
        AssertEqual(
            twoObservations[0].SemanticFingerprint,
            twoObservations[1].SemanticFingerprint,
            "Semantic fingerprint ignores EventId sequence and timestamps");
        AssertEqual(
            false,
            string.Equals(
                twoObservations[0].EventId,
                twoObservations[1].EventId,
                StringComparison.Ordinal),
            "Distinct callback events retain their canonical EventIds");

        WireV1.CommunicationCallbackEnvelope differentPayload =
            CreatePenaltyEnvelope(3, 8);
        handler.ApplyAsync(differentPayload, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        IReadOnlyList<CommunicationCallbackShadowObservation> bounded =
            handler.GetObservationSnapshot();
        AssertEqual(2, bounded.Count, "Observation ledger remains bounded");
        AssertEqual((ulong)2, bounded[0].Sequence, "Oldest observation is evicted");
        AssertEqual((ulong)3, bounded[1].Sequence, "Newest observation is retained");
        AssertEqual(
            (long)1,
            handler.EvictedObservations,
            "Observation eviction remains measurable");
        AssertEqual(
            false,
            string.Equals(
                bounded[0].SemanticFingerprint,
                bounded[1].SemanticFingerprint,
                StringComparison.Ordinal),
            "Different semantic payloads produce different fingerprints");
        AssertEqual((long)3, handler.ObservedCallbacks, "Total observations remain cumulative");
        AssertEqual((ulong)3, handler.LastObservedSequence, "Last sequence tracks the newest callback");

        handler.EndStream();
        AssertEqual(
            false,
            handler.IsStreamActive,
            "Ending typed observation clears its active state");
        AssertThrows<InvalidOperationException>(
            () => handler.ApplyAsync(
                    CreatePenaltyEnvelope(4, 9),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult(),
            "Observation without an active stream fails closed");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackSemanticFingerprint.Compute(
                CreateBarrier(generation, 3, 3, 0)),
            "Replay barrier cannot enter semantic observation fingerprints");

        const string nextGeneration =
            "22222222-3333-4444-5555-666666666666";
        handler.BeginStream(nextGeneration, 3);
        AssertEqual(
            0,
            handler.GetObservationSnapshot().Count,
            "A new typed stream clears evidence from the prior generation");
        AssertEqual(
            (long)0,
            handler.ObservedCallbacks,
            "A new typed stream resets cumulative window observations");
        AssertEqual(
            (long)0,
            handler.EvictedObservations,
            "A new typed stream resets cumulative window evictions");
        AssertEqual(
            (ulong)0,
            handler.LastObservedSequence,
            "A new typed stream resets the prior sequence");
        handler.EndStream();

        AssertThrows<ArgumentOutOfRangeException>(
            () => new CommunicationCallbackShadowEnvelopeHandler(0),
            "Observation capacity rejects zero");
        AssertThrows<ArgumentOutOfRangeException>(
            () => new CommunicationCallbackShadowEnvelopeHandler(
                CommunicationCallbackShadowEnvelopeHandler
                    .MaximumObservationCapacity + 1),
            "Observation capacity has an absolute ceiling");

        Console.WriteLine(
            "[PASS] Bounded typed callback shadow observation ledger self-test");
    }

    private static WireV1.CommunicationCallbackEnvelope CreatePenaltyEnvelope(
        ulong sequence,
        int penaltyLogId)
    {
        return new WireV1.CommunicationCallbackEnvelope
        {
            EventId = Guid.NewGuid().ToString("D"),
            Sequence = sequence,
            IssuedAtUnixTimeMs = 1_900_000_000_000 + checked((long)sequence),
            ExpiresAtUnixTimeMs = 1_900_000_030_000 + checked((long)sequence),
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

    private static WireV1.CommunicationCallbackEnvelope CreateBarrier(
        string generation,
        ulong replayThroughSequence,
        ulong resumeAfterSequence,
        uint replayedEvents)
    {
        return new WireV1.CommunicationCallbackEnvelope
        {
            Sequence = replayThroughSequence,
            ReplayComplete = new WireV1.CommunicationCallbackReplayComplete
            {
                RuntimeGenerationId = generation,
                ReplayThroughSequence = replayThroughSequence,
                ResumeAfterSequence = resumeAfterSequence,
                ReplayedEvents = replayedEvents
            }
        };
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
