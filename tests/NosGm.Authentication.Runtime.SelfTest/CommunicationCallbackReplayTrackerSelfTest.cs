using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackReplayTrackerSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        string generation =
            "11111111-2222-3333-4444-555555555555";
        DateTimeOffset completedAt =
            new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero);
        var tracker = new CommunicationCallbackReplayTracker();

        tracker.BeginStream(generation, 0);
        CommunicationCallbackReplayEvidence emptyEvidence = tracker.Complete(
            CreateBarrier(generation, 0, 0, 0),
            completedAt);
        AssertEqual(true, tracker.IsComplete, "An empty replay reaches readiness");
        AssertEqual(
            generation,
            emptyEvidence.RuntimeGenerationId,
            "Replay evidence preserves the runtime generation");
        AssertEqual(
            (ulong)0,
            emptyEvidence.ReplayThroughSequence,
            "An empty runtime may publish a zero replay boundary");
        AssertEqual(
            completedAt,
            emptyEvidence.CompletedAt,
            "Replay evidence records completion time");
        AssertThrows(
            () => tracker.Complete(
                CreateBarrier(generation, 0, 0, 0),
                completedAt),
            "Duplicate replay barriers fail closed");

        tracker.Reset();
        AssertEqual(false, tracker.IsComplete, "A disconnected stream is not ready");
        AssertEqual(
            null,
            tracker.Evidence,
            "Reset removes stale replay evidence");

        tracker.BeginStream(generation, 5);
        tracker.ObserveCallbackBeforeBarrier(7);
        tracker.ObserveCallbackBeforeBarrier(9);
        CommunicationCallbackReplayEvidence replayEvidence = tracker.Complete(
            CreateBarrier(generation, 10, 5, 2),
            completedAt);
        AssertEqual(
            (ulong)5,
            replayEvidence.ResumeAfterSequence,
            "Replay evidence preserves the durable resume cursor");
        AssertEqual(
            (uint)2,
            replayEvidence.ReplayedEvents,
            "Replay evidence counts only callback envelopes before the barrier");
        tracker.ValidateLiveSequence(11);
        AssertThrows(
            () => tracker.ValidateLiveSequence(10),
            "A live callback cannot cross backwards over the barrier");

        tracker.Reset();
        tracker.BeginStream(generation, 5);
        tracker.ObserveCallbackBeforeBarrier(7);
        AssertThrows(
            () => tracker.Complete(
                CreateBarrier(generation, 7, 5, 0),
                completedAt),
            "Barrier replay counts must match observed callback envelopes");

        tracker.Reset();
        tracker.BeginStream(generation, 5);
        WireV1.CommunicationCallbackEnvelope malformed =
            CreateBarrier(generation, 5, 5, 0);
        malformed.EventId = Guid.NewGuid().ToString("D");
        AssertThrows(
            () => tracker.Complete(malformed, completedAt),
            "Replay barriers cannot contain event metadata");

        tracker.Reset();
        tracker.BeginStream(generation, 5);
        AssertThrows(
            () => tracker.Complete(
                CreateBarrier(
                    "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                    5,
                    5,
                    0),
                completedAt),
            "Replay barriers cannot switch runtime generations");

        Console.WriteLine(
            "[PASS] Communication callback replay tracker self-test");
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

    private static void AssertThrows(Action action, string name)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("[PASS] " + name);
            return;
        }

        throw new InvalidOperationException(name + ": no exception was thrown.");
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
