using System.Runtime.CompilerServices;
using NosGm.Authentication.Client.Configuration;

internal static class ConfigurationUpdateCursorSelfTest
{
    private const string Generation1 =
        "11111111-2222-3333-4444-555555555555";
    private const string Generation2 =
        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    [ModuleInitializer]
    public static void Run()
    {
        var cursor = new ConfigurationUpdateCursor();
        ConfigurationTransportUpdate first = Update(Generation1, 4);
        AssertEqual(
            ConfigurationUpdateCursorDecision.Accepted,
            cursor.Inspect(first, allowSnapshotRecovery: true),
            "Configuration snapshot recovery seeds the generation cursor");
        cursor.Commit(first);

        AssertEqual(
            ConfigurationUpdateCursorDecision.Duplicate,
            cursor.Inspect(Update(Generation1, 4), false),
            "Configuration cursor rejects overlap duplicates");
        AssertEqual(
            ConfigurationUpdateCursorDecision.Accepted,
            cursor.Inspect(Update(Generation1, 5), false),
            "Configuration cursor accepts the next live generation");
        AssertEqual(
            ConfigurationUpdateCursorDecision.Gap,
            cursor.Inspect(Update(Generation1, 7), false),
            "Configuration cursor detects a live generation gap");
        AssertEqual(
            ConfigurationUpdateCursorDecision.RuntimeChanged,
            cursor.Inspect(Update(Generation2, 2), false),
            "Live Configuration stream cannot cross runtime generations");
        AssertEqual(
            ConfigurationUpdateCursorDecision.Accepted,
            cursor.Inspect(Update(Generation2, 2), true),
            "Snapshot recovery accepts a new runtime generation");
        cursor.Commit(Update(Generation2, 2));
        AssertEqual(2UL, cursor.Generation,
            "Configuration cursor resets its numeric generation after restart");
        AssertEqual(Generation2, cursor.RuntimeGenerationId,
            "Configuration cursor binds recovery to the new runtime identity");
        AssertEqual(
            ConfigurationUpdateCursorDecision.Stale,
            cursor.Inspect(Update(Generation2, 1), true),
            "Configuration recovery rejects a stale generation");

        Console.WriteLine("[PASS] Configuration update cursor self-test");
    }

    private static ConfigurationTransportUpdate Update(
        string runtimeGenerationId,
        ulong generation)
    {
        return new ConfigurationTransportUpdate
        {
            RuntimeGenerationId = runtimeGenerationId,
            Generation = generation,
            Configuration = new ConfigurationTransportSnapshot
            {
                MaxGold = 2_000_000_000L,
                TimeExpBuffUnixTimeMilliseconds = 1_700_000_000_000L,
                TimeGoldBuffUnixTimeMilliseconds = 1_700_000_010_000L
            }
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
