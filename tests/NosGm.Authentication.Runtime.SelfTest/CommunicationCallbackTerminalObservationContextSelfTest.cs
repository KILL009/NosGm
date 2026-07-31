using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackTerminalObservationContextSelfTest
{
    private const string Generation =
        "11111111-2222-3333-4444-555555555555";

    [ModuleInitializer]
    public static void Run()
    {
        VerifySynchronousTerminalSnapshot();
        VerifyContextCleanupAfterCallbackFailure();

        Console.WriteLine(
            "[PASS] Terminal typed callback observation context self-test");
    }

    private static void VerifySynchronousTerminalSnapshot()
    {
        CommunicationCallbackTerminalTypedObservationWindow captured = null;
        int callbackCount = 0;
        CommunicationCallbackShadowEnvelopeHandler handler = null;
        handler = new CommunicationCallbackShadowEnvelopeHandler(
            streamEnded: () =>
            {
                callbackCount++;
                captured =
                    CommunicationCallbackTerminalObservationContext
                        .CurrentTypedWindow;
                AssertEqual(false, captured == null,
                    "Terminal typed evidence is visible during the synchronous end callback");
                AssertEqual(false, handler.IsStreamActive,
                    "The typed stream is terminal before SCS closure runs");
            });
        var tracker = new CommunicationCallbackReplayTracker();

        handler.BeginStream(Generation, 0);
        tracker.BeginStream(Generation, 0);
        WireV1.CommunicationCallbackEnvelope replay =
            CreatePenaltyEnvelope(1, 7);
        handler.ApplyAsync(replay, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        tracker.ObserveCallbackBeforeBarrier(replay.Sequence);
        CommunicationCallbackReplayEvidence replayEvidence =
            tracker.Complete(
                CreateBarrier(Generation, 1, 0, 1),
                DateTimeOffset.UtcNow);
        handler.CompleteReplay(replayEvidence);
        handler.ApplyAsync(
                CreatePenaltyEnvelope(2, 8),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        handler.EndStream();

        AssertEqual(1, callbackCount,
            "One active stream produces exactly one terminal callback");
        AssertEqual(true, captured != null,
            "The end callback retained its immutable terminal snapshot");
        AssertEqual(Generation, captured.RuntimeGenerationId,
            "Terminal typed evidence preserves the runtime generation");
        AssertEqual(replayEvidence, captured.ReplayEvidence,
            "Terminal typed evidence preserves the replay barrier");
        AssertEqual((long)2, captured.ObservedCallbacks,
            "Terminal typed evidence preserves cumulative observations");
        AssertEqual((long)0, captured.EvictedObservations,
            "Terminal typed evidence preserves eviction accounting");
        AssertEqual(2, captured.GetObservationSnapshot().Count,
            "Terminal typed evidence retains replay and live observations");
        AssertEqual(
            CommunicationCallbackObservationPhase.Live,
            captured.GetObservationSnapshot()[1].Phase,
            "Terminal typed evidence preserves live phase classification");
        AssertEqual(
            true,
            captured.EndedAt != default(DateTimeOffset),
            "Terminal typed evidence records its closure time");
        AssertEqual(
            true,
            CommunicationCallbackTerminalObservationContext
                .CurrentTypedWindow == null,
            "Terminal typed evidence is cleared after the synchronous callback");

        handler.EndStream();
        AssertEqual(1, callbackCount,
            "Repeated end calls cannot duplicate terminal evidence");
    }

    private static void VerifyContextCleanupAfterCallbackFailure()
    {
        var handler = new CommunicationCallbackShadowEnvelopeHandler(
            streamEnded: () =>
                throw new InvalidOperationException("expected callback failure"));
        handler.BeginStream(Generation, 0);

        AssertThrows<InvalidOperationException>(
            handler.EndStream,
            "A terminal callback failure remains observable to its caller");
        AssertEqual(
            true,
            CommunicationCallbackTerminalObservationContext
                .CurrentTypedWindow == null,
            "Terminal context cleanup survives callback failure");
        AssertEqual(false, handler.IsStreamActive,
            "Terminal callback failure cannot resurrect the ended stream");
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
