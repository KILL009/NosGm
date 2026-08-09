using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class CommunicationCallbackMigrationMapSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        string repositoryRoot = FindRepositoryRoot();
        string interfacePath = Path.Combine(
            repositoryRoot,
            "Data",
            "NosGm.Master.Library",
            "Interface",
            "ICommunicationClient.cs");
        string mapPath = Path.Combine(
            repositoryRoot,
            "contracts",
            "cluster",
            "v1",
            "communication-callback-migration-map.json");
        string protoPath = Path.Combine(
            repositoryRoot,
            "contracts",
            "cluster",
            "v1",
            "cluster_communication_callbacks.proto");
        string projectPath = Path.Combine(
            repositoryRoot,
            "Data",
            "NosGm.Cluster.Contracts",
            "NosGm.Cluster.Contracts.csproj");

        string interfaceSource = File.ReadAllText(interfacePath);
        string[] interfaceMethods = GetInterfaceMethodNames(
            interfaceSource,
            "ICommunicationClient");

        using JsonDocument map = JsonDocument.Parse(File.ReadAllText(mapPath));
        JsonElement root = map.RootElement;
        AssertEqual(
            2,
            root.GetProperty("schemaVersion").GetInt32(),
            "Callback migration map schema version");
        AssertEqual(
            "ICommunicationClient",
            root.GetProperty("legacyInterface").GetString(),
            "Callback map names the remaining SCS interface");
        AssertEqual(
            "ClusterCommunicationCallbacks",
            root.GetProperty("targetService").GetString(),
            "Callback map names the typed streaming service");
        AssertEqual(
            "bounded server-streaming subscription with replay cursor",
            root.GetProperty("deliveryModel").GetString(),
            "Callback map records the delivery model");

        var mappedMethods = new List<string>();
        int typedCount = 0;
        int deferredCount = 0;
        string deferredMethod = null;
        foreach (JsonElement entry in root.GetProperty("methods").EnumerateArray())
        {
            mappedMethods.Add(
                entry.GetProperty("legacyMethod").GetString());
            string disposition =
                entry.GetProperty("disposition").GetString();
            if (string.Equals(
                    disposition,
                    "typed_stream_event",
                    StringComparison.Ordinal))
            {
                typedCount++;
                AssertEqual(
                    "Master",
                    entry.GetProperty("publisherRole").GetString(),
                    "Every remaining typed callback is published only by Master");
            }
            else if (string.Equals(
                         disposition,
                         "deferred",
                         StringComparison.Ordinal))
            {
                deferredCount++;
                deferredMethod =
                    entry.GetProperty("legacyMethod").GetString();
            }
            else
            {
                throw new InvalidOperationException(
                    "Unknown remaining callback migration disposition: " +
                    disposition);
            }
        }

        AssertSequenceEqual(
            interfaceMethods,
            mappedMethods.OrderBy(method => method, StringComparer.Ordinal)
                .ToArray(),
            "Every remaining legacy callback has an explicit disposition");
        AssertEqual(
            10,
            typedCount,
            "Ten remaining callback methods have typed shadow stream events");
        AssertEqual(
            1,
            deferredCount,
            "Exactly one remaining callback is deferred");
        AssertEqual(
            "SendMessageToCharacter",
            deferredMethod,
            "Raw character messaging remains deferred to a dedicated typed slice");

        JsonElement completed = root.GetProperty("completed");
        AssertEqual(
            1,
            completed.GetArrayLength(),
            "Exactly one callback authority cutover is complete");
        JsonElement penalty = completed[0];
        AssertEqual(
            "UpdatePenaltyLog",
            penalty.GetProperty("legacyMethod").GetString(),
            "PenaltyRefresh records its retired SCS method");
        AssertEqual(
            "grpc_authoritative",
            penalty.GetProperty("disposition").GetString(),
            "PenaltyRefresh is gRPC authoritative");
        AssertEqual(
            true,
            penalty.GetProperty("legacySurfaceRemoved").GetBoolean(),
            "PenaltyRefresh legacy callback surface is removed");
        AssertEqual(
            JsonValueKind.Null,
            penalty.GetProperty("fallback").ValueKind,
            "PenaltyRefresh has no transport fallback");
        AssertEqual(
            "PenaltyRefreshCallback",
            penalty.GetProperty("target").GetString(),
            "PenaltyRefresh uses its typed payload");
        AssertEqual(
            "ALL_NODES",
            penalty.GetProperty("targetKind").GetString(),
            "PenaltyRefresh targets Login and World nodes");
        AssertEqual(
            false,
            interfaceMethods.Contains(
                "UpdatePenaltyLog",
                StringComparer.Ordinal),
            "Retired PenaltyRefresh is absent from ICommunicationClient");

        string[] subscriberRoles = penalty
            .GetProperty("subscriberRoles")
            .EnumerateArray()
            .Select(role => role.GetString())
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        AssertSequenceEqual(
            new[] { "Login", "World" },
            subscriberRoles,
            "PenaltyRefresh remains routed to Login and World");

        string proto = File.ReadAllText(protoPath);
        AssertContains(
            proto,
            "service ClusterCommunicationCallbacks",
            "Typed callback service is declared");
        AssertContains(
            proto,
            "returns (stream CommunicationCallbackEnvelope)",
            "Login and World callbacks use server streaming");
        AssertContains(
            proto,
            "rpc PublishCommunicationCallback",
            "Master publishes through a typed unary RPC");
        AssertContains(
            proto,
            "resume_after_sequence",
            "Subscribers carry a replay cursor");
        AssertContains(
            proto,
            "oneof callback",
            "Callback payloads use an explicit typed union");
        AssertNotContains(
            proto,
            "bytes payload",
            "Callback transport contains no untyped payload");
        AssertNotContains(
            proto,
            "rpc Invoke",
            "Callback transport contains no reflection-style invocation");
        AssertNotContains(
            proto,
            "SCSCharacterMessage",
            "Legacy CLR message DTOs stay off the wire");
        AssertNotContains(
            proto,
            "string message",
            "Already-rendered client message packets stay off the callback wire");

        string project = File.ReadAllText(projectPath);
        AssertContains(
            project,
            "cluster_communication_callbacks.proto",
            "Callback client and server stubs participate in dual-target generation");

        Console.WriteLine(
            "[PASS] Communication callback migration inventory self-test");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string marker = Path.Combine(
                directory.FullName,
                "contracts",
                "cluster",
                "v1",
                "cluster_communication_callbacks.proto");
            if (File.Exists(marker))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate the NosGM repository root for callback contract tests.");
    }

    private static string[] GetInterfaceMethodNames(
        string source,
        string interfaceName)
    {
        string withoutComments = Regex.Replace(
            source,
            "//.*?$|/\\*.*?\\*/",
            string.Empty,
            RegexOptions.Multiline | RegexOptions.Singleline);
        Match interfaceMatch = Regex.Match(
            withoutComments,
            "public\\s+interface\\s+" +
            Regex.Escape(interfaceName) +
            "\\b[^{]*\\{(?<body>.*?)\\r?\\n\\s*\\}",
            RegexOptions.Singleline);
        if (!interfaceMatch.Success)
        {
            throw new InvalidOperationException(
                "Unable to locate interface " + interfaceName + ".");
        }

        return interfaceMatch.Groups["body"].Value
            .Split(';')
            .Where(statement =>
                statement.IndexOf('(') >= 0 &&
                statement.IndexOf(')') >= 0)
            .Select(statement =>
                Regex.Match(
                    statement,
                    "([A-Za-z_][A-Za-z0-9_]*)\\s*\\("))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AssertContains(
        string content,
        string expected,
        string name)
    {
        if (!content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                name + ": missing '" + expected + "'.");
        }

        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertNotContains(
        string content,
        string forbidden,
        string name)
    {
        if (content.Contains(forbidden, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                name + ": contains forbidden '" + forbidden + "'.");
        }

        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertSequenceEqual(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual,
        string name)
    {
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                name + ": expected [" + string.Join(",", expected) +
                "] but received [" + string.Join(",", actual) + "].");
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
