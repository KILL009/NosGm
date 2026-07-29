using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.State;
using WireV1 = NosGm.Cluster.Wire.V1;

internal static class CommunicationStateSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var values = new Dictionary<string, string>
        {
            [CommunicationRuntimeOptions.MaximumAccountsVariable] = "10",
            [CommunicationRuntimeOptions.MaximumWorldsVariable] = "10",
            [CommunicationRuntimeOptions.SessionTtlVariable] = "60",
            [CommunicationRuntimeOptions.GlacernonPortVariable] = "5100"
        };
        CommunicationRuntimeOptions options =
            CommunicationRuntimeOptions.Load(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(values)
                    .Build());
        var time = new CommunicationMutableTimeProvider(
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var state = new ClusterCommunicationState(options, time);
        Guid worldOne =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid worldTwo =
            Guid.Parse("22222222-3333-4444-5555-666666666666");
        Guid glacernon =
            Guid.Parse("33333333-4444-5555-6666-777777777777");

        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.RegisterWorldServer(
                worldOne,
                "127.0.0.1",
                1337,
                100,
                "S2-Sumeria").Result,
            "Communication state registers the first World");
        AssertEqual(
            1,
            state.RegisterWorldServer(
                worldOne,
                "127.0.0.1",
                1337,
                100,
                "S2-Sumeria").ChannelId,
            "Identical World registration is idempotent");
        AssertEqual(
            2,
            state.RegisterWorldServer(
                worldTwo,
                "127.0.0.1",
                1338,
                100,
                "S2-Sumeria").ChannelId,
            "World channels are allocated deterministically per group");
        AssertEqual(
            CommunicationRuntimeOptions.GlacernonChannelId,
            state.RegisterWorldServer(
                glacernon,
                "127.0.0.1",
                5100,
                100,
                "S2-Sumeria").ChannelId,
            "Glacernon retains its special channel identity");
        AssertEqual(
            2,
            state.ListVisibleWorldServers().Count,
            "The Login projection excludes the Glacernon channel");

        const long accountId = 42;
        const int sessionId = 50219;
        const long characterId = 10004;
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.RegisterAccountLogin(
                accountId,
                sessionId,
                "127.0.0.1"),
            "Login registers an account/session tuple");
        AssertEqual(
            true,
            state.IsAccountSessionRegistered(accountId, sessionId),
            "The exact account/session tuple is visible");
        AssertEqual(
            true,
            state.IsLoginPermitted(accountId, sessionId),
            "A detached registered session may enter World");
        AssertEqual(
            WireV1.CommunicationResultCode.NotFound,
            state.ConnectAccount(worldOne, accountId, sessionId + 1),
            "A mismatched SessionID cannot attach to World");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.ConnectAccount(worldOne, accountId, sessionId),
            "World attaches the exact account/session tuple");
        AssertEqual(
            true,
            state.IsAccountConnected(accountId),
            "The attached account is reported connected");
        AssertEqual(
            false,
            state.IsLoginPermitted(accountId, sessionId),
            "An attached account cannot repeat the normal World handoff");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.ConnectCharacter(
                worldOne,
                accountId,
                sessionId,
                characterId),
            "World attaches the selected character");
        AssertEqual(
            1,
            state.ListVisibleWorldServers()[0].ConnectedAccounts,
            "Typed World snapshots include bounded connection counts");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.DisconnectAccount(
                accountId,
                sessionId,
                true),
            "Gameforge reselection preserves the exact registration");
        AssertEqual(
            true,
            state.IsAccountSessionRegistered(accountId, sessionId),
            "Preserved reselection keeps the stable SessionID");
        AssertEqual(
            true,
            state.IsLoginPermitted(accountId, sessionId),
            "Preserved reselection may receive a fresh World handoff");

        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.ConnectAccount(worldTwo, accountId, sessionId),
            "The preserved session can attach to another channel");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.ConnectCharacter(
                worldTwo,
                accountId,
                sessionId,
                characterId),
            "The preserved session can select a character again");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.DisconnectCharacter(
                worldTwo,
                accountId,
                sessionId,
                characterId),
            "Character disconnect detaches the World but preserves the login tuple");
        AssertEqual(
            true,
            state.IsLoginPermitted(accountId, sessionId),
            "Character disconnect returns the tuple to Login-permitted state");

        time.Advance(TimeSpan.FromSeconds(61));
        AssertEqual(
            false,
            state.IsAccountSessionRegistered(accountId, sessionId),
            "Unpulsed communication sessions expire after the bounded TTL");
        AssertEqual(
            0,
            state.AccountCount,
            "Expired account registrations are removed from state");

        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            state.UnregisterWorldServer(worldOne),
            "World unregister succeeds exactly once");
        AssertEqual(
            WireV1.CommunicationResultCode.NotFound,
            state.UnregisterWorldServer(worldOne),
            "Repeated World unregister fails closed");

        Console.WriteLine(
            "[PASS] Communication state runtime lifecycle self-test");
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

    private sealed class CommunicationMutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public CommunicationMutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
