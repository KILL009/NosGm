using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackShadowWorldSelfTest
{
    private const string RuntimeGeneration =
        "11111111-2222-3333-4444-555555555555";
    private const string WorldId =
        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    [ModuleInitializer]
    public static void Run()
    {
        WireV1.RegisterCommunicationCallbackShadowWorldRequest register =
            CreateRegister(WireV1.ClusterNodeRole.World);
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            CommunicationCallbackShadowWorldContractValidator.ValidateRegister(
                register),
            "World may register one callback-only shadow route");

        register.Context.CallerRole = WireV1.ClusterNodeRole.Login;
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallerRole,
            CommunicationCallbackShadowWorldContractValidator.ValidateRegister(
                register),
            "Login cannot register a callback-only World route");
        register.Context.CallerRole = WireV1.ClusterNodeRole.World;

        register.RuntimeGenerationId = Guid.Empty.ToString("D");
        AssertEqual(
            CommunicationCallbackContractValidationError
                .InvalidSubscriberIdentity,
            CommunicationCallbackShadowWorldContractValidator.ValidateRegister(
                register),
            "Shadow World registration rejects an empty runtime generation");
        register.RuntimeGenerationId = RuntimeGeneration;

        register.ChannelId = 0;
        AssertEqual(
            CommunicationCallbackContractValidationError
                .InvalidSubscriberIdentity,
            CommunicationCallbackShadowWorldContractValidator.ValidateRegister(
                register),
            "Shadow World registration requires the assigned channel");
        register.ChannelId = 1;

        var unregister =
            new WireV1.UnregisterCommunicationCallbackShadowWorldRequest
            {
                Context = CreateContext(WireV1.ClusterNodeRole.World),
                RuntimeGenerationId = RuntimeGeneration,
                WorldId = WorldId
            };
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            CommunicationCallbackShadowWorldContractValidator
                .ValidateUnregister(unregister),
            "World may remove its callback-only shadow route");
        unregister.WorldId = "not-a-world-id";
        AssertEqual(
            CommunicationCallbackContractValidationError
                .InvalidSubscriberIdentity,
            CommunicationCallbackShadowWorldContractValidator
                .ValidateUnregister(unregister),
            "Shadow World removal rejects malformed identity");
    }

    private static WireV1.RegisterCommunicationCallbackShadowWorldRequest
        CreateRegister(WireV1.ClusterNodeRole role)
    {
        return new WireV1.RegisterCommunicationCallbackShadowWorldRequest
        {
            Context = CreateContext(role),
            RuntimeGenerationId = RuntimeGeneration,
            WorldId = WorldId,
            ChannelId = 1,
            WorldGroup = "Sumeria"
        };
    }

    private static WireV1.RequestContext CreateContext(
        WireV1.ClusterNodeRole role)
    {
        return new WireV1.RequestContext
        {
            Version = new WireV1.ProtocolVersion
            {
                Major = ClusterContractVersion.CurrentMajor,
                Minor = ClusterContractVersion.CurrentMinor
            },
            RequestId = Guid.NewGuid().ToString("D"),
            IssuedAtUnixTimeMs = 1_900_000_000_000,
            DeadlineUnixTimeMs = 1_900_000_010_000,
            CallerRole = role,
            RequestedService = WireV1.ClusterService.Communication,
            CallerInstanceId = "callback-shadow-world-self-test"
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
}
