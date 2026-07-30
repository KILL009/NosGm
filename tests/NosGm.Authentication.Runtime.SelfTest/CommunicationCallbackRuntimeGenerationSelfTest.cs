using System.Runtime.CompilerServices;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackRuntimeGenerationSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        var time = new FixedTimeProvider(
            new DateTimeOffset(2034, 5, 6, 7, 8, 9, TimeSpan.Zero));
        var first = new CommunicationCallbackRuntimeIdentity(time);
        var second = new CommunicationCallbackRuntimeIdentity(time);
        AssertEqual(
            false,
            first.GenerationId == second.GenerationId,
            "Every callback runtime process receives a distinct generation");
        AssertEqual(
            time.GetUtcNow(),
            first.StartedAt,
            "Callback runtime generation records its process start time");

        WireV1.GetCommunicationCallbackRuntimeInfoRequest loginRequest =
            CreateRequest(WireV1.ClusterNodeRole.Login);
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            CommunicationCallbackRuntimeInfoContractValidator.Validate(
                loginRequest),
            "Login may query callback runtime generation metadata");
        WireV1.GetCommunicationCallbackRuntimeInfoRequest worldRequest =
            CreateRequest(WireV1.ClusterNodeRole.World);
        AssertEqual(
            CommunicationCallbackContractValidationError.None,
            CommunicationCallbackRuntimeInfoContractValidator.Validate(
                worldRequest),
            "World may query callback runtime generation metadata");
        WireV1.GetCommunicationCallbackRuntimeInfoRequest masterRequest =
            CreateRequest(WireV1.ClusterNodeRole.Master);
        AssertEqual(
            CommunicationCallbackContractValidationError.InvalidCallerRole,
            CommunicationCallbackRuntimeInfoContractValidator.Validate(
                masterRequest),
            "Master cannot impersonate a callback subscriber generation query");
    }

    private static WireV1.GetCommunicationCallbackRuntimeInfoRequest
        CreateRequest(WireV1.ClusterNodeRole role)
    {
        return new WireV1.GetCommunicationCallbackRuntimeInfoRequest
        {
            Context = new WireV1.RequestContext
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
                CallerInstanceId = "runtime-generation-self-test"
            }
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
