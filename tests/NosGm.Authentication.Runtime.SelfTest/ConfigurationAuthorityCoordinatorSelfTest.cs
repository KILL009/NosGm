using NosGm.Authentication.Client.Configuration;
using System.Runtime.CompilerServices;

internal static class ConfigurationAuthorityCoordinatorSelfTest
{
    private const string RuntimeA =
        "10000000-0000-0000-0000-000000000001";
    private const string RuntimeB =
        "10000000-0000-0000-0000-000000000002";
    private const string RuntimeC =
        "10000000-0000-0000-0000-000000000003";
    private const string RuntimeD =
        "10000000-0000-0000-0000-000000000004";
    private const string RuntimeE =
        "10000000-0000-0000-0000-000000000005";

    [ModuleInitializer]
    internal static void Run()
    {
        DefaultAndQualificationRemainScsAuthoritative();
        OperatorControlsAreStrictAndImmutable();
        ActivationAndRecoverySwitchEveryOperationTogether();
        OverlapSuppressesSemanticTwinsInEitherArrivalOrder();
        RepeatedIdenticalUpdatesRemainOccurrenceBound();
        RuntimeDriftAndTypedFailureRollBack();
        CapacityExhaustionFailsClosedWithoutLosingScs();
        RollbackRetainsPendingTwinSuppression();
        MalformedRoutingQueriesFailClosed();
        Console.WriteLine(
            "[PASS] Configuration joint authority and overlap guard self-test");
    }

    private static void DefaultAndQualificationRemainScsAuthoritative()
    {
        var coordinator = new ConfigurationAuthorityCoordinator();
        QualifiedEvidence evidence = BuildQualifiedEvidence();

        AssertAllOperationsSelected(
            coordinator,
            ConfigurationAuthoritySource.Scs,
            "SCS owns Get, Update and callback by default");
        AssertEqual(
            false,
            coordinator.ObserveQualification(
                evidence.Reports.Take(2).ToArray()),
            "Unconfigured Configuration authority cannot arm");
        AssertEqual(
            true,
            coordinator.Configure(
                evidence.ProcessGenerationId,
                NewOperatorOptions(),
                effectRoutingEnabled: false),
            "Immutable operator controls configure dry-run authority once");
        AssertEqual(
            false,
            coordinator.ObserveQualification(
                evidence.Reports.Take(2).ToArray()),
            "Fewer than three parity runtimes cannot arm Configuration authority");
        AssertEqual(
            true,
            coordinator.ObserveQualification(evidence.Reports),
            "Three distinct parity runtimes arm Configuration authority");
        AssertEqual(
            ConfigurationAuthorityState.Armed,
            coordinator.GetStatus().State,
            "Qualification arms without changing live authority");
        AssertAllOperationsSelected(
            coordinator,
            ConfigurationAuthoritySource.Scs,
            "Armed Configuration authority remains entirely on SCS");
        AssertEqual(
            false,
            coordinator.ObserveRuntimeGeneration(
                evidence.ProcessGenerationId,
                RuntimeC),
            "A qualification runtime cannot activate Configuration authority");
    }

    private static void OperatorControlsAreStrictAndImmutable()
    {
        AssertThrows<InvalidOperationException>(
            () => ConfigurationAuthorityOperatorOptions.Load(
                variableName => variableName ==
                    ConfigurationAuthorityOperatorOptions.ArmRequestVariable
                        ? " 20000000-0000-0000-0000-000000000001"
                        : null),
            "Configuration arm request rejects surrounding whitespace");
        AssertThrows<InvalidOperationException>(
            () => ConfigurationAuthorityOperatorOptions.Load(
                variableName => variableName ==
                    ConfigurationAuthorityOperatorOptions.ArmRequestVariable
                        ? "20000000-0000-0000-0000-000000000001"
                        : variableName ==
                            ConfigurationAuthorityOperatorOptions
                                .RollbackRequestVariable
                            ? "true"
                            : null),
            "Configuration arm and rollback controls are mutually exclusive");

        QualifiedEvidence evidence = BuildQualifiedEvidence();
        var coordinator = new ConfigurationAuthorityCoordinator();
        ConfigurationAuthorityOperatorOptions options =
            NewOperatorOptions();
        AssertEqual(
            true,
            coordinator.Configure(
                evidence.ProcessGenerationId,
                options,
                effectRoutingEnabled: false),
            "Configuration operator controls bind to one process generation");
        AssertEqual(
            false,
            coordinator.Configure(
                evidence.ProcessGenerationId,
                options,
                effectRoutingEnabled: false),
            "Identical Configuration operator controls are idempotent");
        AssertThrows<InvalidOperationException>(
            () => coordinator.Configure(
                evidence.ProcessGenerationId,
                options,
                effectRoutingEnabled: true),
            "Configuration effect-routing mutation fails closed inside one process");
        AssertEqual(
            true,
            coordinator.GetStatus().Blocked,
            "Configuration operator mutation blocks the process terminally");
    }

    private static void ActivationAndRecoverySwitchEveryOperationTogether()
    {
        QualifiedCoordinator qualified = NewQualifiedCoordinator();
        AssertEqual(
            true,
            qualified.Coordinator.ObserveRuntimeGeneration(
                qualified.ProcessGenerationId,
                RuntimeD),
            "A fourth runtime generation activates the joint authority gate");
        AssertEqual(
            ConfigurationAuthorityState.TypedGrpcAuthoritative,
            qualified.Coordinator.GetStatus().State,
            "Activation records typed authority before opening ingress");
        AssertAllOperationsSelected(
            qualified.Coordinator,
            ConfigurationAuthoritySource.Scs,
            "Recovery keeps Get, Update and callback together on SCS");

        AssertEqual(
            true,
            qualified.Coordinator.CompleteRecovery(RuntimeD),
            "Active runtime recovery opens typed Configuration ingress");
        AssertAllOperationsSelected(
            qualified.Coordinator,
            ConfigurationAuthoritySource.TypedGrpc,
            "Recovery atomically selects typed Get, Update and callback");
    }

    private static void OverlapSuppressesSemanticTwinsInEitherArrivalOrder()
    {
        QualifiedCoordinator scsFirst = ActivatedCoordinator();
        ConfigurationTransportSnapshot first = NewSnapshot(5000001);
        int scsEffects = 0;
        int typedEffects = 0;
        AssertEqual(
            true,
            scsFirst.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                first,
                () => scsEffects++),
            "SCS-first overlap applies the first semantic Configuration update");
        AssertEqual(
            false,
            scsFirst.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                first,
                () => typedEffects++),
            "Typed semantic twin is suppressed after SCS-first overlap");
        AssertEqual(1, scsEffects,
            "SCS-first overlap executes exactly one effect");
        AssertEqual(0, typedEffects,
            "Suppressed typed twin cannot execute an effect");

        QualifiedCoordinator typedFirst = ActivatedCoordinator();
        ConfigurationTransportSnapshot second = NewSnapshot(5000002);
        AssertEqual(
            true,
            typedFirst.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                second,
                () => typedEffects++),
            "Typed-first overlap applies the first semantic Configuration update");
        AssertEqual(
            false,
            typedFirst.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                second,
                () => scsEffects++),
            "SCS semantic twin is suppressed after typed-first overlap");
        AssertEqual(1, typedEffects,
            "Typed-first overlap executes exactly one effect");
        AssertEqual(1, scsEffects,
            "Suppressed SCS twin cannot execute another effect");
    }

    private static void RepeatedIdenticalUpdatesRemainOccurrenceBound()
    {
        QualifiedCoordinator active = ActivatedCoordinator();
        ConfigurationTransportSnapshot snapshot = NewSnapshot(6000001);
        int effects = 0;

        for (int occurrence = 0; occurrence < 2; occurrence++)
        {
            AssertEqual(
                true,
                active.Coordinator.TryApplyCallback(
                    ConfigurationAuthoritySource.Scs,
                    snapshot,
                    () => effects++),
                "Each repeated Configuration occurrence applies once");
            AssertEqual(
                false,
                active.Coordinator.TryApplyCallback(
                    ConfigurationAuthoritySource.TypedGrpc,
                    snapshot,
                    () => effects++),
                "Each repeated Configuration occurrence suppresses one twin");
        }

        AssertEqual(2, effects,
            "Two identical occurrences remain two effects, never four");
        AssertEqual(
            2L,
            active.Coordinator.GetStatus().OverlapDuplicatesSuppressed,
            "Overlap diagnostics count each suppressed occurrence");
    }

    private static void RuntimeDriftAndTypedFailureRollBack()
    {
        QualifiedCoordinator drift = ActivatedCoordinator();
        AssertEqual(
            false,
            drift.Coordinator.ObserveRuntimeGeneration(
                drift.ProcessGenerationId,
                RuntimeE),
            "Runtime drift cannot silently inherit typed Configuration authority");
        AssertEqual(
            ConfigurationAuthorityState.RolledBack,
            drift.Coordinator.GetStatus().State,
            "Runtime drift makes Configuration rollback terminal");
        AssertAllOperationsSelected(
            drift.Coordinator,
            ConfigurationAuthoritySource.Scs,
            "Runtime drift restores every Configuration operation to SCS");

        QualifiedCoordinator failure = ActivatedCoordinator();
        AssertThrows<InvalidOperationException>(
            () => failure.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                NewSnapshot(7000001),
                () => throw new InvalidOperationException(
                    "typed Configuration effect failed")),
            "A typed Configuration callback failure remains observable");
        AssertEqual(
            ConfigurationAuthorityState.RolledBack,
            failure.Coordinator.GetStatus().State,
            "A typed Configuration callback failure rolls back first");
    }

    private static void CapacityExhaustionFailsClosedWithoutLosingScs()
    {
        QualifiedCoordinator active = ActivatedCoordinator(overlapCapacity: 1);
        int typedEffects = 0;
        int scsEffects = 0;
        AssertEqual(
            true,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                NewSnapshot(8000001),
                () => typedEffects++),
            "The bounded overlap ledger accepts its first typed update");
        AssertEqual(
            false,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                NewSnapshot(8000002),
                () => typedEffects++),
            "Overlap capacity rejects another typed update before its effect");
        AssertEqual(1, typedEffects,
            "Rejected typed capacity overflow cannot execute gameplay effect");
        AssertEqual(
            ConfigurationAuthorityState.RolledBack,
            active.Coordinator.GetStatus().State,
            "Overlap capacity saturation fails closed to rollback");
        AssertEqual(
            true,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                NewSnapshot(8000002),
                () => scsEffects++),
            "SCS still applies the rejected typed update after rollback");
        AssertEqual(1, scsEffects,
            "Capacity rollback does not lose the authoritative SCS effect");
    }

    private static void RollbackRetainsPendingTwinSuppression()
    {
        QualifiedCoordinator active = ActivatedCoordinator();
        ConfigurationTransportSnapshot pendingTwin = NewSnapshot(9000001);
        int effects = 0;
        AssertEqual(
            true,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.TypedGrpc,
                pendingTwin,
                () => effects++),
            "A typed overlap effect can be pending when rollback begins");
        AssertEqual(
            true,
            active.Coordinator.RequestRollback(),
            "Explicit Configuration rollback closes active typed authority");
        AssertEqual(
            false,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                pendingTwin,
                () => effects++),
            "Rollback suppresses a delayed SCS twin already applied by typed gRPC");
        AssertEqual(
            true,
            active.Coordinator.TryApplyCallback(
                ConfigurationAuthoritySource.Scs,
                NewSnapshot(9000002),
                () => effects++),
            "Rollback immediately accepts a new authoritative SCS update");
        AssertEqual(2, effects,
            "Rollback preserves one prior typed effect and one new SCS effect");
    }

    private static void MalformedRoutingQueriesFailClosed()
    {
        var coordinator = new ConfigurationAuthorityCoordinator();
        AssertThrows<InvalidOperationException>(
            () => coordinator.ShouldUse(
                (ConfigurationAuthoritySource)0,
                ConfigurationAuthorityOperation.Get),
            "Unknown Configuration authority sources fail closed");
        AssertThrows<InvalidOperationException>(
            () => coordinator.ShouldUse(
                ConfigurationAuthoritySource.Scs,
                (ConfigurationAuthorityOperation)0),
            "Unknown Configuration authority operations fail closed");
        AssertThrows<InvalidOperationException>(
            () => coordinator.TryApplyCallback(
                (ConfigurationAuthoritySource)0,
                NewSnapshot(10000001),
                () => { }),
            "Unknown Configuration callback sources fail closed");
    }

    private static QualifiedCoordinator NewQualifiedCoordinator(
        int overlapCapacity =
            ConfigurationUpdateOverlapDeduplicationLedger.DefaultCapacity)
    {
        QualifiedEvidence evidence = BuildQualifiedEvidence();
        var coordinator = new ConfigurationAuthorityCoordinator(
            ConfigurationAuthorityGate.DefaultRequiredParityWindows,
            overlapCapacity);
        AssertEqual(
            true,
            coordinator.Configure(
                evidence.ProcessGenerationId,
                NewOperatorOptions(),
                effectRoutingEnabled: true),
            "Qualified Configuration coordinator enables effect routing explicitly");
        AssertEqual(
            true,
            coordinator.ObserveQualification(evidence.Reports),
            "Qualified Configuration evidence arms the coordinator");
        return new QualifiedCoordinator(
            coordinator,
            evidence.ProcessGenerationId);
    }

    private static QualifiedCoordinator ActivatedCoordinator(
        int overlapCapacity =
            ConfigurationUpdateOverlapDeduplicationLedger.DefaultCapacity)
    {
        QualifiedCoordinator qualified =
            NewQualifiedCoordinator(overlapCapacity);
        AssertEqual(
            true,
            qualified.Coordinator.ObserveRuntimeGeneration(
                qualified.ProcessGenerationId,
                RuntimeD),
            "The unqualified runtime activates Configuration authority");
        AssertEqual(
            true,
            qualified.Coordinator.CompleteRecovery(RuntimeD),
            "Recovery opens activated Configuration authority");
        return qualified;
    }

    private static QualifiedEvidence BuildQualifiedEvidence()
    {
        var ledger = new ConfigurationUpdateObservationLedger(
            64,
            TimeSpan.FromMilliseconds(100));
        string[] runtimes = { RuntimeA, RuntimeB, RuntimeC };
        var reports = new List<ConfigurationUpdateParityReport>();
        DateTimeOffset evaluatedAt = new DateTimeOffset(
            2030,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);

        for (int index = 0; index < runtimes.Length; index++)
        {
            long marker = 20000000 + index;
            ledger.RecordGrpc(
                NewUpdate(NewSnapshot(marker), 1, runtimes[index],
                    recovered: true));
            ledger.RecordScs(NewSnapshot(marker + 100));
            ledger.RecordGrpc(
                NewUpdate(NewSnapshot(marker + 100), 2, runtimes[index]));
            ConfigurationUpdateParityReport report = ledger.EvaluateParity(
                evaluatedAt.AddMinutes(index));
            AssertEqual(
                ConfigurationUpdateParityVerdict.Parity,
                report.Verdict,
                "Each Configuration qualification runtime proves parity");
            reports.Add(report);
        }

        return new QualifiedEvidence(
            ledger.ProcessGenerationId,
            reports.AsReadOnly());
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

    private static ConfigurationAuthorityOperatorOptions NewOperatorOptions()
    {
        return ConfigurationAuthorityOperatorOptions.Load(
            variableName => variableName ==
                ConfigurationAuthorityOperatorOptions.ArmRequestVariable
                    ? "20000000-0000-0000-0000-000000000001"
                    : null);
    }

    private static ConfigurationTransportUpdate NewUpdate(
        ConfigurationTransportSnapshot snapshot,
        ulong generation,
        string runtimeGenerationId,
        bool recovered = false)
    {
        return new ConfigurationTransportUpdate
        {
            Configuration = snapshot,
            Generation = generation,
            RuntimeGenerationId = runtimeGenerationId,
            RecoveredFromSnapshot = recovered
        };
    }

    private static void AssertAllOperationsSelected(
        ConfigurationAuthorityCoordinator coordinator,
        ConfigurationAuthoritySource selected,
        string name)
    {
        foreach (ConfigurationAuthorityOperation operation in
                 new[]
                 {
                     ConfigurationAuthorityOperation.Get,
                     ConfigurationAuthorityOperation.Update,
                     ConfigurationAuthorityOperation.Callback
                 })
        {
            AssertEqual(
                true,
                coordinator.ShouldUse(selected, operation),
                name + " (" + operation + ")");
            ConfigurationAuthoritySource other =
                selected == ConfigurationAuthoritySource.Scs
                    ? ConfigurationAuthoritySource.TypedGrpc
                    : ConfigurationAuthoritySource.Scs;
            AssertEqual(
                false,
                coordinator.ShouldUse(other, operation),
                name + " excludes " + other + " (" + operation + ")");
        }
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

    private sealed class QualifiedEvidence
    {
        public QualifiedEvidence(
            string processGenerationId,
            IReadOnlyList<ConfigurationUpdateParityReport> reports)
        {
            ProcessGenerationId = processGenerationId;
            Reports = reports;
        }

        public string ProcessGenerationId { get; }

        public IReadOnlyList<ConfigurationUpdateParityReport> Reports { get; }
    }

    private sealed class QualifiedCoordinator
    {
        public QualifiedCoordinator(
            ConfigurationAuthorityCoordinator coordinator,
            string processGenerationId)
        {
            Coordinator = coordinator;
            ProcessGenerationId = processGenerationId;
        }

        public ConfigurationAuthorityCoordinator Coordinator { get; }

        public string ProcessGenerationId { get; }
    }
}
