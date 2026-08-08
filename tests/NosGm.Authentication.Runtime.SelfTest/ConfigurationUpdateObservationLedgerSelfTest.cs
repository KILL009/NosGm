using NosGm.Authentication.Client.Configuration;
using System.Runtime.CompilerServices;

internal static class ConfigurationUpdateObservationLedgerSelfTest
{
    [ModuleInitializer]
    internal static void Run()
    {
        FingerprintsMatchAcrossTransports();
        TypedPhasesRemainExplicit();
        SourceOrdinalsRemainIndependent();
        RetentionIsBounded();
        MalformedTypedObservationsFailClosed();
        Console.WriteLine(
            "[PASS] Bounded Configuration SCS-versus-gRPC observation ledger self-test");
    }

    private static void FingerprintsMatchAcrossTransports()
    {
        var ledger = new ConfigurationUpdateObservationLedger(8);
        ConfigurationTransportSnapshot snapshot = NewSnapshot(1000000);
        ConfigurationUpdateObservation scs = ledger.RecordScs(snapshot);
        ConfigurationUpdateObservation grpc = ledger.RecordGrpc(
            NewUpdate(snapshot, 7));

        AssertEqual(
            scs.SemanticFingerprint,
            grpc.SemanticFingerprint,
            "Equivalent SCS and gRPC Configuration payloads share one semantic fingerprint");
        AssertEqual(
            64,
            scs.SemanticFingerprint.Length,
            "Configuration semantic fingerprints retain SHA-256 length");
        AssertEqual(
            ledger.ProcessGenerationId,
            grpc.ProcessGenerationId,
            "Both transports share one process-scoped observation identity");
    }

    private static void TypedPhasesRemainExplicit()
    {
        var ledger = new ConfigurationUpdateObservationLedger(8);
        ConfigurationTransportSnapshot snapshot = NewSnapshot(2000000);
        ConfigurationUpdateObservation recovery = ledger.RecordGrpc(
            NewUpdate(snapshot, 1, recovered: true));
        ConfigurationUpdateObservation replay = ledger.RecordGrpc(
            NewUpdate(snapshot, 2, replayed: true));
        ConfigurationUpdateObservation live = ledger.RecordGrpc(
            NewUpdate(snapshot, 3));

        AssertEqual(
            ConfigurationUpdateObservationPhase.Recovery,
            recovery.Phase,
            "Snapshot recovery is not mistaken for live callback parity");
        AssertEqual(
            ConfigurationUpdateObservationPhase.Replay,
            replay.Phase,
            "Retained replay is not mistaken for live callback parity");
        AssertEqual(
            ConfigurationUpdateObservationPhase.Live,
            live.Phase,
            "A new typed generation is recorded as live");
    }

    private static void SourceOrdinalsRemainIndependent()
    {
        var ledger = new ConfigurationUpdateObservationLedger(8);
        ConfigurationTransportSnapshot snapshot = NewSnapshot(3000000);
        ConfigurationUpdateObservation scsFirst = ledger.RecordScs(snapshot);
        ConfigurationUpdateObservation grpcFirst = ledger.RecordGrpc(
            NewUpdate(snapshot, 10));
        ConfigurationUpdateObservation scsSecond = ledger.RecordScs(snapshot);

        AssertEqual(1UL, scsFirst.SourceOrdinal,
            "The first SCS observation starts its own FIFO sequence");
        AssertEqual(1UL, grpcFirst.SourceOrdinal,
            "The first gRPC observation starts its own FIFO sequence");
        AssertEqual(2UL, scsSecond.SourceOrdinal,
            "The second SCS observation advances only the SCS sequence");
        AssertEqual(3UL, scsSecond.LedgerOrdinal,
            "The combined ledger preserves cross-transport arrival order");
    }

    private static void RetentionIsBounded()
    {
        var ledger = new ConfigurationUpdateObservationLedger(2);
        ledger.RecordScs(NewSnapshot(1));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(2), 1));
        ledger.RecordScs(NewSnapshot(3));

        IReadOnlyList<ConfigurationUpdateObservation> retained =
            ledger.GetObservationSnapshot();
        AssertEqual(2, retained.Count,
            "Configuration observation retention remains bounded");
        AssertEqual(1L, ledger.EvictedObservations,
            "Configuration observation evidence loss is counted explicitly");
        AssertEqual(2UL, retained[0].LedgerOrdinal,
            "The oldest Configuration observation is evicted first");
    }

    private static void MalformedTypedObservationsFailClosed()
    {
        var ledger = new ConfigurationUpdateObservationLedger(4);
        AssertThrows<InvalidOperationException>(
            () => ledger.RecordGrpc(
                new ConfigurationTransportUpdate
                {
                    Configuration = NewSnapshot(5),
                    Generation = 1,
                    RuntimeGenerationId = "not-a-guid"
                }),
            "A malformed runtime generation cannot enter parity evidence");
        AssertThrows<InvalidOperationException>(
            () => ledger.RecordGrpc(
                NewUpdate(NewSnapshot(6), 2, replayed: true, recovered: true)),
            "One typed observation cannot be both replay and recovery");
        AssertThrows<InvalidOperationException>(
            () => ledger.RecordScs(NewSnapshot(0)),
            "An invalid SCS Configuration snapshot cannot enter parity evidence");
    }

    private static ConfigurationTransportSnapshot NewSnapshot(long maxGold)
    {
        return new ConfigurationTransportSnapshot
        {
            MaxGold = maxGold,
            TimeExpBuffUnixTimeMilliseconds = 1700000000000,
            TimeGoldBuffUnixTimeMilliseconds = 1700000001000
        };
    }

    private static ConfigurationTransportUpdate NewUpdate(
        ConfigurationTransportSnapshot snapshot,
        ulong generation,
        bool replayed = false,
        bool recovered = false)
    {
        return new ConfigurationTransportUpdate
        {
            Configuration = snapshot,
            Generation = generation,
            RuntimeGenerationId =
                "b95ed76b-d6c3-4b30-9132-cf606e46fdd6",
            Replayed = replayed,
            RecoveredFromSnapshot = recovered
        };
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', received '" + actual + "'.");
        }
        Console.WriteLine("[PASS] " + name);
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
}
