using System.Runtime.CompilerServices;
using NosGm.Authentication.Server.State;
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
