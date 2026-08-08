using System.Runtime.CompilerServices;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Configuration.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class ClusterConfigurationStateSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var state = new ClusterConfigurationState();
        AssertFalse(
            state.TryGet(out _),
            "Shadow Configuration state starts unavailable");

        var first = new WireV1.ConfigurationSnapshot
        {
            MaxGold = 2_000_000_000L,
            TimeExpBuffUnixTimeMs = 1_700_000_000_000L,
            TimeGoldBuffUnixTimeMs = 1_700_000_010_000L
        };
        ClusterConfigurationState.SnapshotState firstState =
            state.Update(first);
        AssertEqual(1UL, firstState.Generation, "First Configuration generation");
        AssertEqual(first.MaxGold, firstState.Configuration.MaxGold,
            "First Configuration value is stored");

        first.MaxGold = 1;
        AssertTrue(
            state.TryGet(out ClusterConfigurationState.SnapshotState stored),
            "Stored Configuration snapshot is available");
        AssertEqual(2_000_000_000L, stored.Configuration.MaxGold,
            "Input mutation cannot alter stored Configuration state");

        stored.Configuration.MaxGold = 2;
        AssertTrue(
            state.TryGet(out ClusterConfigurationState.SnapshotState reread),
            "Configuration snapshot can be reread");
        AssertEqual(2_000_000_000L, reread.Configuration.MaxGold,
            "Returned snapshot mutation cannot alter stored Configuration state");
        AssertEqual(1UL, reread.Generation,
            "Read does not advance Configuration generation");

        var duplicate = new WireV1.ConfigurationSnapshot
        {
            MaxGold = 2_000_000_000L,
            TimeExpBuffUnixTimeMs = 1_700_000_000_000L,
            TimeGoldBuffUnixTimeMs = 1_700_000_010_000L
        };
        ClusterConfigurationState.SnapshotState duplicateState =
            state.Update(duplicate);
        AssertEqual(1UL, duplicateState.Generation,
            "Equivalent Configuration update preserves generation");

        var second = new WireV1.ConfigurationSnapshot
        {
            MaxGold = 3_000_000_000L,
            TimeExpBuffUnixTimeMs = 1_700_000_020_000L,
            TimeGoldBuffUnixTimeMs = 1_700_000_030_000L
        };
        ClusterConfigurationState.SnapshotState secondState =
            state.Update(second);
        AssertEqual(2UL, secondState.Generation,
            "Changed Configuration update advances generation");
        AssertEqual(second.MaxGold, secondState.Configuration.MaxGold,
            "Latest Configuration update wins");

        AssertEqual(
            ConfigurationSubscriptionOpenResult.Success,
            state.TryOpenSubscription(
                "configuration-state-world-1",
                0,
                out ClusterConfigurationSubscription subscription),
            "Configuration subscriber opens from generation zero");
        AssertEqual(2, subscription.ReplayUpdates.Count,
            "Configuration subscriber receives bounded retained replay");
        AssertEqual(1UL, subscription.ReplayUpdates[0].Generation,
            "Configuration replay begins at the first retained generation");
        AssertEqual(2UL, subscription.ReplayUpdates[1].Generation,
            "Configuration replay preserves generation order");

        duplicate = new WireV1.ConfigurationSnapshot
        {
            MaxGold = second.MaxGold,
            TimeExpBuffUnixTimeMs = second.TimeExpBuffUnixTimeMs,
            TimeGoldBuffUnixTimeMs = second.TimeGoldBuffUnixTimeMs
        };
        state.Update(duplicate);
        AssertFalse(
            subscription.PendingUpdates.TryRead(out _),
            "Equivalent Configuration update does not publish a duplicate");

        var third = new WireV1.ConfigurationSnapshot
        {
            MaxGold = 4_000_000_000L,
            TimeExpBuffUnixTimeMs = 1_700_000_040_000L,
            TimeGoldBuffUnixTimeMs = 1_700_000_050_000L
        };
        ClusterConfigurationState.SnapshotState thirdState =
            state.Update(third);
        AssertTrue(
            subscription.PendingUpdates.TryRead(out
                ClusterConfigurationState.SnapshotState live),
            "Changed Configuration update is published live");
        AssertEqual(thirdState.Generation, live.Generation,
            "Live Configuration update preserves its generation");

        AssertEqual(
            ConfigurationSubscriptionOpenResult.Success,
            state.TryOpenSubscription(
                "configuration-state-world-1",
                thirdState.Generation,
                out ClusterConfigurationSubscription replacement),
            "A reconnect replaces the prior process subscription");
        AssertEqual(
            ConfigurationSubscriptionTerminationReason.Superseded,
            subscription.TerminationReason,
            "Replaced Configuration subscription terminates explicitly");
        subscription.DisposeAsync().AsTask().GetAwaiter().GetResult();
        AssertEqual(1, state.SubscriptionCount,
            "Disposing a replaced lease cannot remove its replacement");
        replacement.DisposeAsync().AsTask().GetAwaiter().GetResult();
        AssertEqual(0, state.SubscriptionCount,
            "Disposing the active Configuration lease releases capacity");

        for (int index = 0;
             index < ConfigurationContractLimits.MaxRetainedUpdates + 1;
             index++)
        {
            state.Update(new WireV1.ConfigurationSnapshot
            {
                MaxGold = 5_000_000_000L + index,
                TimeExpBuffUnixTimeMs = 1_700_001_000_000L + index,
                TimeGoldBuffUnixTimeMs = 1_700_002_000_000L + index
            });
        }
        AssertEqual(
            ConfigurationSubscriptionOpenResult.InvalidResumeCursor,
            state.TryOpenSubscription(
                "configuration-state-stale-world",
                1,
                out _),
            "A cursor older than bounded replay must recover from a snapshot");

        Console.WriteLine("[PASS] Cluster configuration state self-test");
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
