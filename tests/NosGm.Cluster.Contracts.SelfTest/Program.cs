using NosGm.Cluster.Contracts.V1;
using WireProtocolVersion = NosGm.Cluster.Wire.V1.ProtocolVersion;

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected '{expected}', received '{actual}'.");
    }

    Console.WriteLine($"[PASS] {name}");
}

const long issuedAt = 1_800_000_000_000;
var valid = new ClusterRequestContext
{
    Version = ClusterContractVersion.Current,
    RequestId = "11111111-2222-3333-4444-555555555555",
    IssuedAtUnixTimeMilliseconds = issuedAt,
    DeadlineUnixTimeMilliseconds =
        issuedAt + ClusterProtocolLimits.DefaultDeadlineMilliseconds,
    CallerRole = ClusterNodeRole.Login,
    RequestedService = ClusterService.Authentication,
    CallerInstanceId = "login-es-1"
};

AssertEqual(
    ClusterContractValidationError.None,
    ClusterContractValidator.Validate(valid),
    "Valid request context");

valid.Version = new ClusterContractVersion(2, 0);
AssertEqual(
    ClusterContractValidationError.UnsupportedVersion,
    ClusterContractValidator.Validate(valid),
    "Future major versions fail closed");
valid.Version = ClusterContractVersion.Current;

valid.RequestId = "not-a-request-id";
AssertEqual(
    ClusterContractValidationError.InvalidRequestId,
    ClusterContractValidator.Validate(valid),
    "Request IDs are canonical GUIDs");
valid.RequestId = "11111111-2222-3333-4444-555555555555";

valid.CallerRole = (ClusterNodeRole)128;
AssertEqual(
    ClusterContractValidationError.InvalidCallerRole,
    ClusterContractValidator.Validate(valid),
    "Unknown caller roles fail closed");
valid.CallerRole = ClusterNodeRole.World;

valid.RequestedService = (ClusterService)99;
AssertEqual(
    ClusterContractValidationError.InvalidService,
    ClusterContractValidator.Validate(valid),
    "Unknown services fail closed");
valid.RequestedService = ClusterService.Communication;

valid.DeadlineUnixTimeMilliseconds =
    issuedAt + ClusterProtocolLimits.MaxDeadlineMilliseconds + 1;
AssertEqual(
    ClusterContractValidationError.InvalidDeadline,
    ClusterContractValidator.Validate(valid),
    "Excessive deadlines fail closed");
valid.DeadlineUnixTimeMilliseconds =
    issuedAt + ClusterProtocolLimits.DefaultDeadlineMilliseconds;

AssertEqual(
    ClusterContractValidationError.None,
    ClusterContractValidator.ValidatePayloadLength(
        ClusterProtocolLimits.MaxInboundMessageBytes),
    "Maximum inbound payload is accepted");
AssertEqual(
    ClusterContractValidationError.PayloadTooLarge,
    ClusterContractValidator.ValidatePayloadLength(
        ClusterProtocolLimits.MaxInboundMessageBytes + 1),
    "Oversized inbound payload is rejected");
AssertEqual(
    ClusterContractValidationError.NegativePayloadLength,
    ClusterContractValidator.ValidatePayloadLength(-1),
    "Negative payload length is rejected");

AssertEqual("1.0", ClusterContractVersion.Current.ToString(), "Version format");

var generatedWireVersion = new WireProtocolVersion
{
    Major = ClusterContractVersion.Current.Major,
    Minor = ClusterContractVersion.Current.Minor
};
AssertEqual((uint)1, generatedWireVersion.Major, "Protobuf code generation");
AssertEqual((uint)0, generatedWireVersion.Minor, "Protobuf minor version");

if (ClusterProtocolLimits.MaxInboundMessageBytes >= 128 * 1024 * 1024)
{
    throw new InvalidOperationException(
        "The new contract must not inherit SCS's 128 MiB frame allowance.");
}

Console.WriteLine("NosGM cluster contract self-test passed.");
