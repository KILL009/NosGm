using System.Runtime.CompilerServices;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.State;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class ConfigurationRuntimeControllerSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var disabled = new ConfigurationRuntimeController(
            new ConfigurationRuntimeControlOptions(false),
            TimeProvider.System);
        disabled.Update(NewSnapshot(100), out Guid disabledGeneration);
        AssertEqual(
            ConfigurationRuntimeRestartResult.Disabled,
            disabled.TryRestart(disabledGeneration, out _),
            "Configuration runtime control is disabled by default");
        AssertEqual(
            disabledGeneration,
            disabled.GetStatus().RuntimeGenerationId,
            "Disabled control cannot rotate the runtime");

        var controller = new ConfigurationRuntimeController(
            new ConfigurationRuntimeControlOptions(true),
            TimeProvider.System);
        var callbackIdentity = new CommunicationCallbackRuntimeIdentity(
            TimeProvider.System);
        Guid callbackGeneration = callbackIdentity.GenerationId;
        ConfigurationRuntimeStatus initial = controller.GetStatus();
        AssertFalse(initial.Seeded,
            "Configuration controller starts unavailable");
        AssertEqual(0U, initial.RestartCount,
            "Configuration controller starts without restarts");

        AssertTrue(
            controller.TrySeed(
                NewSnapshot(200),
                out ClusterConfigurationState.SnapshotState seeded,
                out Guid seededRuntimeGeneration),
            "Master can seed an empty Configuration runtime once");
        AssertEqual(initial.RuntimeGenerationId, seededRuntimeGeneration,
            "Seeding does not rotate the Configuration runtime");
        AssertEqual(1UL, seeded.Generation,
            "Initial Configuration runtime starts at generation one");

        AssertFalse(
            controller.TrySeed(
                NewSnapshot(201),
                out ClusterConfigurationState.SnapshotState existingSeed,
                out Guid duplicateSeedRuntimeGeneration),
            "Master cannot overwrite an already seeded Configuration runtime");
        AssertEqual(200L, existingSeed.Configuration.MaxGold,
            "Rejected Master reseed preserves the authoritative snapshot");
        AssertEqual(seededRuntimeGeneration, duplicateSeedRuntimeGeneration,
            "Rejected Master reseed stays on the same runtime identity");

        AssertEqual(
            ConfigurationSubscriptionOpenResult.Success,
            controller.TryOpenSubscription(
                seededRuntimeGeneration,
                "configuration-runtime-controller-world",
                seeded.Generation,
                out ClusterConfigurationSubscription oldSubscription,
                out Guid openedRuntimeGeneration),
            "World stream opens on the current Configuration runtime");
        AssertEqual(seededRuntimeGeneration, openedRuntimeGeneration,
            "World stream binds the exact Configuration runtime");

        AssertEqual(
            ConfigurationRuntimeRestartResult.RuntimeGenerationChanged,
            controller.TryRestart(Guid.NewGuid(), out _),
            "Stale compare-and-swap restart is rejected");
        AssertEqual(
            ConfigurationSubscriptionTerminationReason.None,
            oldSubscription.TerminationReason,
            "Rejected restart keeps the World stream alive");

        AssertEqual(
            ConfigurationRuntimeRestartResult.Success,
            controller.TryRestart(
                seededRuntimeGeneration,
                out ConfigurationRuntimeStatus restarted),
            "Exact Master request restarts only Configuration runtime");
        AssertTrue(restarted.Seeded,
            "Configuration restart preserves the current snapshot seed");
        AssertEqual(1UL, restarted.ConfigurationGeneration,
            "Configuration restart resets its numeric generation");
        AssertEqual(1U, restarted.RestartCount,
            "Configuration restart count advances once");
        AssertEqual(0, restarted.ActiveSubscriptions,
            "Configuration restart releases old subscriptions");
        AssertNotEqual(seededRuntimeGeneration, restarted.RuntimeGenerationId,
            "Configuration restart creates a distinct runtime generation");
        AssertEqual(
            ConfigurationSubscriptionTerminationReason.RuntimeRestarted,
            oldSubscription.TerminationReason,
            "Configuration restart terminates the old World stream explicitly");
        AssertTrue(
            oldSubscription.TerminationToken.IsCancellationRequested,
            "Configuration restart signals the old World stream boundary");
        AssertEqual(callbackGeneration, callbackIdentity.GenerationId,
            "Configuration restart does not rotate callback runtime");

        AssertTrue(
            controller.TryGet(
                out ClusterConfigurationState.SnapshotState preserved,
                out Guid preservedRuntimeGeneration),
            "Restarted Configuration runtime remains seeded");
        AssertEqual(200L, preserved.Configuration.MaxGold,
            "Restarted Configuration runtime preserves the snapshot");
        AssertEqual(restarted.RuntimeGenerationId, preservedRuntimeGeneration,
            "Restarted reads expose only the new runtime generation");

        AssertEqual(
            ConfigurationSubscriptionOpenResult.RuntimeChanged,
            controller.TryOpenSubscription(
                seededRuntimeGeneration,
                "configuration-runtime-controller-stale-world",
                0,
                out _,
                out _),
            "Old runtime cursors fail closed after restart");
        AssertEqual(
            ConfigurationSubscriptionOpenResult.Success,
            controller.TryOpenSubscription(
                restarted.RuntimeGenerationId,
                "configuration-runtime-controller-recovered-world",
                0,
                out ClusterConfigurationSubscription recoveredSubscription,
                out _),
            "World can recover on the restarted Configuration runtime");
        AssertEqual(1, recoveredSubscription.ReplayUpdates.Count,
            "Restarted Configuration runtime replays its preserved seed");

        oldSubscription.DisposeAsync().AsTask().GetAwaiter().GetResult();
        recoveredSubscription.DisposeAsync().AsTask().GetAwaiter().GetResult();
        Console.WriteLine("[PASS] Configuration runtime controller self-test");
    }

    private static WireV1.ConfigurationSnapshot NewSnapshot(long marker)
    {
        return new WireV1.ConfigurationSnapshot
        {
            MaxGold = marker,
            TimeExpBuffUnixTimeMs = 1_700_000_000_000L + marker,
            TimeGoldBuffUnixTimeMs = 1_700_100_000_000L + marker
        };
    }

    private static void AssertTrue(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name + ": expected true.");
        }
        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertFalse(bool value, string name)
    {
        if (value)
        {
            throw new InvalidOperationException(name + ": expected false.");
        }
        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertNotEqual<T>(T left, T right, string name)
    {
        if (EqualityComparer<T>.Default.Equals(left, right))
        {
            throw new InvalidOperationException(name + ": values must differ.");
        }
        Console.WriteLine("[PASS] " + name);
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
