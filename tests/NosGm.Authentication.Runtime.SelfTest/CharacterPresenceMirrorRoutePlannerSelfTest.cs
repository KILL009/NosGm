using System.Runtime.CompilerServices;
using NosGm.Communication.Client;

internal static class CharacterPresenceMirrorRoutePlannerSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        Guid source = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid peerA = Guid.Parse("20000000-0000-0000-0000-000000000001");
        Guid peerB = Guid.Parse("30000000-0000-0000-0000-000000000001");
        Guid otherGroup =
            Guid.Parse("40000000-0000-0000-0000-000000000001");

        IReadOnlyList<Guid> peers =
            CharacterPresenceMirrorRoutePlanner.ResolvePeerWorldIds(
                new CommunicationCallbackWorldRoute[]
                {
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = peerB,
                        WorldGroup = "S1"
                    },
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = source,
                        WorldGroup = "S1"
                    },
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = peerA,
                        WorldGroup = "S1"
                    },
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = peerA,
                        WorldGroup = "S1"
                    },
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = otherGroup,
                        WorldGroup = "S2"
                    },
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = Guid.Empty,
                        WorldGroup = "S1"
                    },
                    null
                },
                source);

        AssertSequence(
            new[] { peerA, peerB },
            peers,
            "Presence routing returns unique deterministic peers from the source group");
        AssertEqual(
            false,
            peers.Contains(source),
            "Presence routing excludes the source World");
        AssertEqual(
            false,
            peers.Contains(otherGroup),
            "Presence routing excludes another World group");

        IReadOnlyList<Guid> unknownSource =
            CharacterPresenceMirrorRoutePlanner.ResolvePeerWorldIds(
                new[]
                {
                    new CommunicationCallbackWorldRoute
                    {
                        WorldId = peerA,
                        WorldGroup = "S1"
                    }
                },
                source);
        AssertEqual(
            0,
            unknownSource.Count,
            "Presence routing returns no peers for an unknown source World");

        AssertThrows<ArgumentException>(
            () => CharacterPresenceMirrorRoutePlanner.ResolvePeerWorldIds(
                Array.Empty<CommunicationCallbackWorldRoute>(),
                Guid.Empty),
            "Presence routing rejects an empty source World ID");
        AssertThrows<ArgumentNullException>(
            () => CharacterPresenceMirrorRoutePlanner.ResolvePeerWorldIds(
                null,
                source),
            "Presence routing rejects a missing World inventory");
    }

    private static void AssertSequence<T>(
        IReadOnlyList<T> expected,
        IReadOnlyList<T> actual,
        string name)
    {
        if (expected.Count != actual.Count ||
            !expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                name + ": received '" + string.Join(",", actual) + "'.");
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
            name + ": no " + typeof(TException).Name + " was thrown.");
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
