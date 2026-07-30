using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackSubscriberSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        VerifySubscriberOptions();
        VerifyPostApplicationCursorCommit();
        VerifyFileCursorStore();
    }

    private static void VerifySubscriberOptions()
    {
        string certificatePath = Path.GetFullPath(
            "callback-subscriber-self-test.pfx");
        string cursorPath = Path.GetFullPath(
            "callback-subscriber-self-test.cursor");
        var values = new Dictionary<string, string>
        {
            [CommunicationCallbackSubscriberOptions.CertificatePathVariable] =
                certificatePath,
            [CommunicationCallbackSubscriberOptions.CursorPathVariable] =
                cursorPath,
            [CommunicationCallbackSubscriberOptions.CallerInstanceIdVariable] =
                "world-callback-self-test-1"
        };
        Guid worldId = Guid.Parse(
            "11111111-2222-3333-4444-555555555555");

        CommunicationCallbackSubscriberOptions options =
            CommunicationCallbackSubscriberOptions.Load(
                ClusterNodeRole.World,
                worldId,
                1,
                "Sumeria",
                name => values.TryGetValue(name, out string value)
                    ? value
                    : null);
        AssertEqual(
            ClusterNodeRole.World,
            options.CallerRole,
            "Callback subscriber retains the World role");
        AssertEqual(
            worldId,
            options.WorldId,
            "Callback subscriber retains its registered World ID");
        AssertEqual(
            AuthenticationGrpcWireMode.Http2,
            options.WireMode,
            "Windows 11 callback subscriber defaults to native HTTP/2");
        AssertEqual(
            cursorPath,
            options.CursorPath,
            "Callback subscriber uses an explicit durable cursor path");

        values[CommunicationCallbackSubscriberOptions.WireModeVariable] =
            "GRPCWEB";
        CommunicationCallbackSubscriberOptions compatibilityOptions =
            CommunicationCallbackSubscriberOptions.Load(
                ClusterNodeRole.World,
                worldId,
                1,
                "Sumeria",
                name => values.TryGetValue(name, out string value)
                    ? value
                    : null);
        AssertEqual(
            AuthenticationGrpcWireMode.GrpcWeb,
            compatibilityOptions.WireMode,
            "gRPC-Web remains an explicit compatibility mode");
        values.Remove(CommunicationCallbackSubscriberOptions.WireModeVariable);

        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackSubscriberOptions.Load(
                ClusterNodeRole.Master,
                Guid.Empty,
                0,
                string.Empty,
                _ => null),
            "Master cannot open a callback subscriber stream");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackSubscriberOptions.Load(
                ClusterNodeRole.Login,
                worldId,
                1,
                "Sumeria",
                name => values.TryGetValue(name, out string value)
                    ? value
                    : null),
            "Login callback identity cannot borrow World fields");
    }

    private static void VerifyPostApplicationCursorCommit()
    {
        var store = new RecordingCursorStore(10);
        var handler = new RecordingEnvelopeHandler();
        var processor = new CommunicationCallbackProcessor(store, handler);
        DateTimeOffset now = new DateTimeOffset(
            2033,
            1,
            2,
            3,
            4,
            5,
            TimeSpan.Zero);

        bool applied = processor.ProcessAsync(
                CreateEnvelope(11, now.AddMinutes(1)),
                now,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        AssertEqual(true, applied, "A new callback is applied");
        AssertEqual(1, handler.Calls, "The callback handler runs once");
        AssertEqual(
            (ulong)11,
            store.Sequence,
            "The callback cursor advances after the handler returns");
        AssertEqual(
            1,
            store.Saves,
            "A successful callback commits one cursor write");

        bool duplicate = processor.ProcessAsync(
                CreateEnvelope(11, now.AddMinutes(1)),
                now,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        AssertEqual(
            false,
            duplicate,
            "An already applied callback is ignored locally");
        AssertEqual(
            1,
            handler.Calls,
            "A duplicate sequence never re-enters the handler");

        bool expired = processor.ProcessAsync(
                CreateEnvelope(12, now.AddSeconds(-1)),
                now,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        AssertEqual(true, expired, "An expired callback advances the stream");
        AssertEqual(
            1,
            handler.Calls,
            "An expired callback is not applied");
        AssertEqual(
            (ulong)12,
            store.Sequence,
            "Expired callback sequence is durably skipped");

        handler.Failure = new InvalidOperationException(
            "intentional callback application failure");
        AssertThrows<InvalidOperationException>(
            () => processor.ProcessAsync(
                    CreateEnvelope(13, now.AddMinutes(1)),
                    now,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult(),
            "A callback handler failure is surfaced");
        AssertEqual(
            (ulong)12,
            store.Sequence,
            "A failed callback never advances the durable cursor");
    }

    private static void VerifyFileCursorStore()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "nosgm-callback-cursor-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "world.cursor");
        try
        {
            var store = new FileCommunicationCallbackCursorStore(path);
            AssertEqual(
                (ulong)0,
                store.Load(),
                "A missing callback cursor starts at zero");
            store.Save(123);
            AssertEqual(
                (ulong)123,
                store.Load(),
                "The file callback cursor survives its first commit");
            store.Save(456);
            AssertEqual(
                (ulong)456,
                store.Load(),
                "The file callback cursor replaces the previous commit");
            File.WriteAllText(path, "not-a-sequence");
            AssertThrows<InvalidOperationException>(
                () => store.Load(),
                "A corrupt callback cursor fails closed");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static WireV1.CommunicationCallbackEnvelope CreateEnvelope(
        ulong sequence,
        DateTimeOffset expiresAt)
    {
        return new WireV1.CommunicationCallbackEnvelope
        {
            EventId = Guid.NewGuid().ToString("D"),
            Sequence = sequence,
            IssuedAtUnixTimeMs = expiresAt.AddMinutes(-1)
                .ToUnixTimeMilliseconds(),
            ExpiresAtUnixTimeMs = expiresAt.ToUnixTimeMilliseconds(),
            PenaltyRefresh = new WireV1.PenaltyRefreshCallback
            {
                PenaltyLogId = checked((int)sequence)
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
        public RecordingCursorStore(ulong sequence)
        {
            Sequence = sequence;
        }

        public ulong Sequence { get; private set; }

        public int Saves { get; private set; }

        public ulong Load()
        {
            return Sequence;
        }

        public void Save(ulong sequence)
        {
            Saves++;
            Sequence = sequence;
        }
    }

    private sealed class RecordingEnvelopeHandler
        : ICommunicationCallbackEnvelopeHandler
    {
        public int Calls { get; private set; }

        public Exception Failure { get; set; }

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (Failure != null)
            {
                throw Failure;
            }
            return Task.CompletedTask;
        }
    }
}
