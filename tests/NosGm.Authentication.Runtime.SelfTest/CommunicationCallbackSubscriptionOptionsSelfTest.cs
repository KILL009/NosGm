using System.Runtime.CompilerServices;
using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Communication;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackSubscriptionOptionsSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        AuthenticationGrpcClientOptions loginTransport = CreateTransport(ClusterNodeRole.Login);
        var login = new CommunicationCallbackSubscriptionOptions(
            loginTransport,
            acceptedKinds: new[] { WireV1.CommunicationCallbackKind.PenaltyRefresh });
        AssertEqual(string.Empty, login.WorldId, "Login callback identity has no World ID");
        AssertEqual(0, login.ChannelId, "Login callback identity has no channel");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackSubscriptionOptions(
                loginTransport,
                Guid.NewGuid().ToString("D"),
                1,
                "S1"),
            "Login cannot claim a World callback identity");

        AuthenticationGrpcClientOptions worldTransport = CreateTransport(ClusterNodeRole.World);
        string worldId = Guid.NewGuid().ToString("D");
        var world = new CommunicationCallbackSubscriptionOptions(
            worldTransport,
            worldId,
            1,
            "S1-Sumeria",
            new[]
            {
                WireV1.CommunicationCallbackKind.CharacterPresence,
                WireV1.CommunicationCallbackKind.KickSession
            });
        AssertEqual(worldId, world.WorldId, "World callback identity preserves World ID");
        AssertEqual(1, world.ChannelId, "World callback identity preserves channel");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationCallbackSubscriptionOptions(
                worldTransport,
                "not-a-guid",
                1,
                "S1-Sumeria"),
            "World callback identity rejects a non-canonical World ID");

        Console.WriteLine("[PASS] Communication callback subscription options self-test");
    }

    private static AuthenticationGrpcClientOptions CreateTransport(ClusterNodeRole role)
    {
        var values = new Dictionary<string, string>
        {
            [AuthenticationGrpcClientOptions.AddressVariable] = "https://127.0.0.1:7443",
            [AuthenticationGrpcClientOptions.CertificatePathVariable] =
                Path.GetFullPath(role + "-callback-self-test.pfx"),
            [AuthenticationGrpcClientOptions.CallerInstanceIdVariable] =
                role.ToString().ToLowerInvariant() + "-callback-self-test",
            [AuthenticationGrpcClientOptions.WireModeVariable] = "GRPCWEB"
        };
        return AuthenticationGrpcClientOptions.Load(
            role,
            name => values.TryGetValue(name, out string value) ? value : null);
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }
        Console.WriteLine($"[PASS] {name}");
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
            Console.WriteLine($"[PASS] {name}");
            return;
        }
        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }
}
