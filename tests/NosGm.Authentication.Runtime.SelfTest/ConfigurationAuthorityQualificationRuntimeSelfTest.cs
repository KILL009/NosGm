using NosGm.Authentication.Client.Configuration;
using System.Runtime.CompilerServices;

internal static class ConfigurationAuthorityQualificationRuntimeSelfTest
{
    private const string RuntimeA =
        "30000000-0000-0000-0000-000000000001";
    private const string RuntimeB =
        "30000000-0000-0000-0000-000000000002";
    private const string RuntimeC =
        "30000000-0000-0000-0000-000000000003";
    private const string RuntimeD =
        "30000000-0000-0000-0000-000000000004";

    [ModuleInitializer]
    internal static void Run()
    {
        DryRunLifecycleExercisesHandshakeWithoutRoutingEffects();
        Console.WriteLine(
            "[PASS] Configuration operator qualification runtime self-test");
    }

    private static void
        DryRunLifecycleExercisesHandshakeWithoutRoutingEffects()
    {
        var coordinator = new ConfigurationAuthorityCoordinator();
        var runtime = new ConfigurationAuthorityQualificationRuntime(
            coordinator,
            evidenceCapacity: 4);
        IReadOnlyList<ConfigurationUpdateParityReport> reports =
            BuildParityReports();
        string processGenerationId = reports[0].ProcessGenerationId;
        AssertEqual(
            true,
            runtime.Configure(
                processGenerationId,
                NewOperatorOptions()),
            "Configuration dry-run lifecycle binds immutable controls");

        AssertEqual(false, runtime.ObserveParity(reports[0]),
            "First Configuration parity runtime is retained");
        AssertEqual(false, runtime.ObserveParity(reports[1]),
            "Second Configuration parity runtime is retained");
        AssertEqual(true, runtime.ObserveParity(reports[2]),
            "Third Configuration parity runtime arms the dry-run gate");
        AssertEqual(
            ConfigurationAuthorityState.Armed,
            coordinator.GetStatus().State,
            "Configuration dry-run reaches armed state from real evidence");

        AssertEqual(
            true,
            runtime.ObserveTypedUpdate(
                processGenerationId,
                NewUpdate(RuntimeD, recovered: true)),
            "Fourth Configuration runtime activates the dry-run handshake");
        AssertEqual(
            ConfigurationAuthorityState.TypedGrpcAuthoritative,
            coordinator.GetStatus().State,
            "Configuration dry-run records the activation generation");
        AssertEqual(
            false,
            coordinator.GetStatus().TypedIngressReady,
            "Disabled effect routing keeps typed ingress closed");
        AssertEqual(
            RuntimeD,
            coordinator.GetStatus().LastRecoveredRuntimeGenerationId,
            "Configuration dry-run records active-runtime recovery");
        AssertEqual(
            true,
            coordinator.ShouldUse(
                ConfigurationAuthoritySource.Scs,
                ConfigurationAuthorityOperation.Callback),
            "Configuration dry-run keeps callback effects on SCS");
        AssertEqual(
            false,
            runtime.ObserveStreamEnded(RuntimeD),
            "Dry-run stream end cannot close an ingress that never opened");
        AssertEqual(
            ConfigurationAuthorityState.TypedGrpcAuthoritative,
            coordinator.GetStatus().State,
            "Dry-run stream end records evidence without routing rollback");
        AssertEqual(
            1L,
            coordinator.GetStatus().StreamEndObservations,
            "Configuration dry-run counts bounded stream-end observations");
        AssertEqual(
            3,
            runtime.GetStatus().RetainedRuntimeCount,
            "Qualification runtime retains one bounded report per runtime");
    }

    private static IReadOnlyList<ConfigurationUpdateParityReport>
        BuildParityReports()
    {
        var ledger = new ConfigurationUpdateObservationLedger(
            64,
            TimeSpan.FromMilliseconds(100));
        string[] runtimes = { RuntimeA, RuntimeB, RuntimeC };
        var reports = new List<ConfigurationUpdateParityReport>();
        DateTimeOffset evaluatedAt = new DateTimeOffset(
            2031,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);

        for (int index = 0; index < runtimes.Length; index++)
        {
            long marker = 31000000 + index;
            ledger.RecordGrpc(
                NewUpdate(runtimes[index], recovered: true, maxGold: marker));
            ledger.RecordScs(NewSnapshot(marker + 100));
            ledger.RecordGrpc(
                NewUpdate(
                    runtimes[index],
                    recovered: false,
                    maxGold: marker + 100,
                    generation: 2));
            ConfigurationUpdateParityReport report = ledger.EvaluateParity(
                evaluatedAt.AddMinutes(index));
            AssertEqual(
                ConfigurationUpdateParityVerdict.Parity,
                report.Verdict,
                "Qualification runtime receives proven Configuration parity");
            reports.Add(report);
        }

        return reports.AsReadOnly();
    }

    private static ConfigurationAuthorityOperatorOptions NewOperatorOptions()
    {
        return ConfigurationAuthorityOperatorOptions.Load(
            variableName => variableName ==
                ConfigurationAuthorityOperatorOptions.ArmRequestVariable
                    ? "40000000-0000-0000-0000-000000000001"
                    : null);
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
        string runtimeGenerationId,
        bool recovered,
        long maxGold = 32000000,
        ulong generation = 1)
    {
        return new ConfigurationTransportUpdate
        {
            Configuration = NewSnapshot(maxGold),
            Generation = generation,
            RuntimeGenerationId = runtimeGenerationId,
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
}
