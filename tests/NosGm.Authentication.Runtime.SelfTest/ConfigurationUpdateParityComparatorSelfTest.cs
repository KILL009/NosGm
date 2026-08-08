using NosGm.Authentication.Client.Configuration;
using System.Runtime.CompilerServices;

internal static class ConfigurationUpdateParityComparatorSelfTest
{
    private const string RuntimeA =
        "b95ed76b-d6c3-4b30-9132-cf606e46fdd6";
    private const string RuntimeB =
        "cc8726bf-0524-4d78-bd17-2c319464ae1d";

    [ModuleInitializer]
    internal static void Run()
    {
        EmptyLedgerWaitsForTypedRuntime();
        RecoveryDoesNotCountAsLiveParity();
        ArrivalSkewSettlesToParity();
        TypedFirstSkewSettlesToParity();
        OrderMismatchFailsClosed();
        CountMismatchRequiresSettlementExpiry();
        RuntimeRotationStartsANewWindow();
        EvictionMakesEvidenceIncomplete();
        SettlementBoundsFailClosed();
        Console.WriteLine(
            "[PASS] Automatic bounded Configuration parity comparator self-test");
    }

    private static void EmptyLedgerWaitsForTypedRuntime()
    {
        var ledger = NewLedger();
        AssertEqual(
            ConfigurationUpdateParityVerdict.WaitingForTypedRuntime,
            ledger.LatestParityReport.Verdict,
            "Configuration parity waits for a typed runtime identity");
        AssertEqual(
            0UL,
            ledger.LatestParityReport.EvaluatedThroughLedgerOrdinal,
            "An empty parity report has no evaluated observations");
    }

    private static void RecoveryDoesNotCountAsLiveParity()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));

        ConfigurationUpdateParityReport report = ledger.LatestParityReport;
        AssertEqual(
            ConfigurationUpdateParityVerdict.NoLiveObservations,
            report.Verdict,
            "Snapshot recovery is evidence but never a live callback match");
        AssertEqual(1, report.GrpcRecoveryCount,
            "Typed recovery observations remain visible in the report");
        AssertEqual(0, report.GrpcLiveCount,
            "Typed recovery cannot inflate the live gRPC count");
    }

    private static void ArrivalSkewSettlesToParity()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordScs(NewSnapshot(2));
        AssertEqual(
            ConfigurationUpdateParityVerdict.InProgress,
            ledger.LatestParityReport.Verdict,
            "An SCS-first delivery remains in progress inside the settlement window");

        ledger.RecordGrpc(NewUpdate(NewSnapshot(2), 2, RuntimeA));
        ConfigurationUpdateParityReport report = ledger.LatestParityReport;
        AssertEqual(
            ConfigurationUpdateParityVerdict.Parity,
            report.Verdict,
            "Equivalent SCS-first and gRPC-second callbacks reach parity");
        AssertEqual(1, report.MatchedLiveCount,
            "One equivalent live callback pair is qualified");
        AssertEqual(3UL, report.EvaluatedThroughLedgerOrdinal,
            "Automatic parity evaluation advances with every observation");
    }

    private static void TypedFirstSkewSettlesToParity()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(3), 2, RuntimeA));
        AssertEqual(
            ConfigurationUpdateParityVerdict.InProgress,
            ledger.LatestParityReport.Verdict,
            "A gRPC-first delivery remains in progress inside the settlement window");

        ledger.RecordScs(NewSnapshot(3));
        AssertEqual(
            ConfigurationUpdateParityVerdict.Parity,
            ledger.LatestParityReport.Verdict,
            "Equivalent gRPC-first and SCS-second callbacks reach parity");
    }

    private static void OrderMismatchFailsClosed()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordScs(NewSnapshot(4));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(5), 2, RuntimeA));

        ConfigurationUpdateParityReport report = ledger.LatestParityReport;
        AssertEqual(
            ConfigurationUpdateParityVerdict.OrderMismatch,
            report.Verdict,
            "Different live semantic fingerprints fail closed as order mismatch");
        AssertEqual(0, report.FirstMismatchIndex,
            "The first semantic mismatch index is retained");
        AssertEqual(1UL, report.ScsOrdinal,
            "The mismatching SCS source ordinal is retained");
        AssertEqual(2UL, report.GrpcGeneration,
            "The mismatching gRPC generation is retained");
    }

    private static void CountMismatchRequiresSettlementExpiry()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordScs(NewSnapshot(6));
        AssertEqual(
            ConfigurationUpdateParityVerdict.InProgress,
            ledger.LatestParityReport.Verdict,
            "A transient callback count skew is not declared a mismatch immediately");

        ConfigurationUpdateParityReport expired = ledger.EvaluateParity(
            DateTimeOffset.UtcNow.AddSeconds(2));
        AssertEqual(
            ConfigurationUpdateParityVerdict.CountMismatch,
            expired.Verdict,
            "An unmatched callback fails closed after settlement expires");
        AssertTrue(
            expired.OldestUnmatchedAgeMilliseconds >= 100,
            "The expired unmatched age is retained for diagnostics");
    }

    private static void RuntimeRotationStartsANewWindow()
    {
        var ledger = NewLedger();
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordScs(NewSnapshot(7));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(7), 2, RuntimeA));
        AssertEqual(
            ConfigurationUpdateParityVerdict.Parity,
            ledger.LatestParityReport.Verdict,
            "The first runtime qualifies one live pair");

        ledger.RecordGrpc(NewUpdate(NewSnapshot(7), 1, RuntimeB,
            recovered: true));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(8), 2, RuntimeB,
            replayed: true));
        AssertEqual(
            ConfigurationUpdateParityVerdict.NoLiveObservations,
            ledger.LatestParityReport.Verdict,
            "A runtime restart starts a fresh live qualification window");

        ledger.RecordScs(NewSnapshot(9));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(9), 3, RuntimeB));
        ConfigurationUpdateParityReport report = ledger.LatestParityReport;
        AssertEqual(
            ConfigurationUpdateParityVerdict.Parity,
            report.Verdict,
            "The replacement runtime qualifies independently");
        AssertEqual(RuntimeB, report.RuntimeGenerationId,
            "The report binds parity to the replacement runtime identity");
        AssertEqual(1, report.GrpcRecoveryCount,
            "Replacement runtime recovery evidence is counted");
        AssertEqual(1, report.GrpcReplayCount,
            "Replacement runtime replay evidence is counted");
        AssertEqual(1, report.IgnoredScsBeforeWindow,
            "Prior-runtime SCS evidence is excluded explicitly");
    }

    private static void EvictionMakesEvidenceIncomplete()
    {
        var ledger = new ConfigurationUpdateObservationLedger(
            2,
            TimeSpan.FromMilliseconds(100));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(1), 1, RuntimeA,
            recovered: true));
        ledger.RecordScs(NewSnapshot(10));
        ledger.RecordGrpc(NewUpdate(NewSnapshot(10), 2, RuntimeA));

        ConfigurationUpdateParityReport report = ledger.LatestParityReport;
        AssertEqual(
            ConfigurationUpdateParityVerdict.IncompleteEvidence,
            report.Verdict,
            "FIFO eviction prevents a false Configuration parity claim");
        AssertEqual(1L, report.EvictedObservations,
            "Parity reports expose lost evidence explicitly");
        AssertTrue(report.HasTerminalMismatch,
            "Incomplete evidence is fail-closed for cutover qualification");
    }

    private static void SettlementBoundsFailClosed()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => new ConfigurationUpdateObservationLedger(
                8,
                TimeSpan.FromMilliseconds(99)),
            "Configuration parity rejects an unsafe settlement window");
    }

    private static ConfigurationUpdateObservationLedger NewLedger()
    {
        return new ConfigurationUpdateObservationLedger(
            32,
            TimeSpan.FromMilliseconds(100));
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
        string runtimeGenerationId,
        bool replayed = false,
        bool recovered = false)
    {
        return new ConfigurationTransportUpdate
        {
            Configuration = snapshot,
            Generation = generation,
            RuntimeGenerationId = runtimeGenerationId,
            Replayed = replayed,
            RecoveredFromSnapshot = recovered
        };
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected +
                "', received '" + actual + "'.");
        }
        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertTrue(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name + ": expected true.");
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
