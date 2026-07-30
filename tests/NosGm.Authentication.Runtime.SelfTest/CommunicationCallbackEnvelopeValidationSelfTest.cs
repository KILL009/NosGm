using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackEnvelopeValidationSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        DateTimeOffset now = new DateTimeOffset(
            2034,
            2,
            3,
            4,
            5,
            6,
            TimeSpan.Zero);

        AssertRejected(
            envelope => envelope.EventId = "NOT-A-CANONICAL-GUID",
            now,
            "Malformed callback event IDs fail before application");
        AssertRejected(
            envelope => envelope.Sequence = (ulong)long.MaxValue + 1UL,
            now,
            "Callback sequences outside the runtime range fail closed");
        AssertRejected(
            envelope => envelope.Target = null,
            now,
            "Callbacks without a target fail closed");
        AssertRejected(
            envelope => envelope.Target.Kind =
                WireV1.CommunicationCallbackTargetKind.AllLoginNodes,
            now,
            "Penalty callbacks cannot narrow the all-node target");
        AssertRejected(
            envelope => envelope.PenaltyRefresh.PenaltyLogId = 0,
            now,
            "Callbacks with invalid payload identity fail closed");
        AssertRejected(
            envelope => envelope.ExpiresAtUnixTimeMs =
                envelope.IssuedAtUnixTimeMs,
            now,
            "Callbacks with an empty lifetime fail closed");
        AssertRejected(
            envelope => envelope.ExpiresAtUnixTimeMs =
                envelope.IssuedAtUnixTimeMs +
                (CommunicationCallbackContractLimits.MaxEventTtlSeconds + 1) *
                1000L,
            now,
            "Callbacks cannot exceed the bounded replay lifetime");

        Console.WriteLine(
            "[PASS] Communication callback envelope validation self-test");
    }

    private static void AssertRejected(
        Action<WireV1.CommunicationCallbackEnvelope> mutate,
        DateTimeOffset now,
        string name)
    {
        var store = new RecordingCursorStore();
        var handler = new RecordingEnvelopeHandler();
        var processor = new CommunicationCallbackProcessor(store, handler);
        WireV1.CommunicationCallbackEnvelope envelope = CreateEnvelope(now);
        mutate(envelope);

        AssertThrows<InvalidOperationException>(
            () => processor.ProcessAsync(
                    envelope,
                    now,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult(),
            name);
        AssertEqual(
            0,
            handler.Calls,
            name + " without entering the handler");
        AssertEqual(
            0,
            store.Saves,
            name + " without advancing the cursor");
    }

    private static WireV1.CommunicationCallbackEnvelope CreateEnvelope(
        DateTimeOffset now)
    {
        return new WireV1.CommunicationCallbackEnvelope
        {
            EventId = Guid.NewGuid().ToString("D"),
            Sequence = 1,
            IssuedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
            ExpiresAtUnixTimeMs = now.AddSeconds(30)
                .ToUnixTimeMilliseconds(),
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
            },
            PenaltyRefresh = new WireV1.PenaltyRefreshCallback
            {
                PenaltyLogId = 7
            }
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

    private static void AssertThrows<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine($"[PASS] {name}");
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }

    private sealed class RecordingCursorStore
        : ICommunicationCallbackCursorStore
    {
        public int Saves { get; private set; }

        public ulong Load()
        {
            return 0;
        }

        public void Save(ulong sequence)
        {
            Saves++;
        }
    }

    private sealed class RecordingEnvelopeHandler
        : ICommunicationCallbackEnvelopeHandler
    {
        public int Calls { get; private set; }

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
