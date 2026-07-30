using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.State;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackShadowWorldRegistrySelfTest
{
    private static readonly Guid WorldId = Guid.Parse(
        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const string Generation =
        "11111111-2222-3333-4444-555555555555";

    [ModuleInitializer]
    public static void Run()
    {
        CommunicationRuntimeOptions options = CommunicationRuntimeOptions.Load(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        [CommunicationRuntimeOptions
                            .MaximumCallbackSubscribersVariable] = "4",
                        [CommunicationRuntimeOptions.MaximumWorldsVariable] =
                            "4",
                        [CommunicationRuntimeOptions.MaximumAccountsVariable] =
                            "100",
                        [CommunicationRuntimeOptions.SessionTtlVariable] = "300"
                    })
                .Build());
        var hub = new CommunicationCallbackHub(
            options,
            new FixedTimeProvider(
                new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        var registry = new CommunicationCallbackShadowWorldRegistry(hub);

        WireV1.RegisterCommunicationCallbackShadowWorldRequest owner =
            CreateRegister("world-shadow-owner");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            registry.Register(owner),
            "World shadow owner registers one callback route");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            registry.Register(owner.Clone()),
            "World shadow registration is idempotent for its owner");

        WireV1.RegisterCommunicationCallbackShadowWorldRequest intruder =
            CreateRegister("world-shadow-intruder");
        AssertEqual(
            WireV1.CommunicationResultCode.Conflict,
            registry.Register(intruder),
            "Another process cannot take over a World shadow route");

        WireV1.SubscribeCommunicationCallbacksRequest subscription =
            CreateSubscription("world-shadow-owner");
        AssertEqual(
            true,
            registry.Owns(subscription),
            "World stream proves exact shadow-route ownership");
        subscription.Context.CallerInstanceId = "world-shadow-intruder";
        AssertEqual(
            false,
            registry.Owns(subscription),
            "Another process cannot subscribe through the owned route");

        WireV1.UnregisterCommunicationCallbackShadowWorldRequest wrongRemoval =
            CreateUnregister("world-shadow-intruder");
        AssertEqual(
            WireV1.CommunicationResultCode.Conflict,
            registry.Unregister(wrongRemoval),
            "Another process cannot remove a World shadow route");
        AssertEqual(
            WireV1.CommunicationResultCode.Success,
            registry.Unregister(CreateUnregister("world-shadow-owner")),
            "World shadow owner removes its callback route");
        AssertEqual(
            WireV1.CommunicationResultCode.NotFound,
            registry.Unregister(CreateUnregister("world-shadow-owner")),
            "World shadow route cleanup is idempotently absent");
    }

    private static WireV1.RegisterCommunicationCallbackShadowWorldRequest
        CreateRegister(string callerInstanceId)
    {
        return new WireV1.RegisterCommunicationCallbackShadowWorldRequest
        {
            Context = CreateContext(callerInstanceId),
            RuntimeGenerationId = Generation,
            WorldId = WorldId.ToString("D"),
            ChannelId = 1,
            WorldGroup = "Sumeria"
        };
    }

    private static WireV1.UnregisterCommunicationCallbackShadowWorldRequest
        CreateUnregister(string callerInstanceId)
    {
        return new WireV1.UnregisterCommunicationCallbackShadowWorldRequest
        {
            Context = CreateContext(callerInstanceId),
            RuntimeGenerationId = Generation,
            WorldId = WorldId.ToString("D")
        };
    }

    private static WireV1.SubscribeCommunicationCallbacksRequest
        CreateSubscription(string callerInstanceId)
    {
        return new WireV1.SubscribeCommunicationCallbacksRequest
        {
            Context = CreateContext(callerInstanceId),
            RuntimeGenerationId = Generation,
            WorldId = WorldId.ToString("D"),
            ChannelId = 1,
            WorldGroup = "Sumeria"
        };
    }

    private static WireV1.RequestContext CreateContext(string callerInstanceId)
    {
        return new WireV1.RequestContext
        {
            CallerRole = WireV1.ClusterNodeRole.World,
            CallerInstanceId = callerInstanceId
        };
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
