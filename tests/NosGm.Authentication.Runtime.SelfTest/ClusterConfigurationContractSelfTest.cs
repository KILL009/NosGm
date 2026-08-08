using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.Configuration.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class ClusterConfigurationContractSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        VerifyGetContract();
        VerifyUpdateContract();
        VerifySubscribeContract();
        VerifySnapshotBounds();

        Console.WriteLine("[PASS] Cluster configuration contract self-test");
    }

    private static void VerifySubscribeContract()
    {
        var request = new WireV1.SubscribeConfigurationUpdatesRequest
        {
            Context = CreateContext(
                WireV1.ClusterNodeRole.World,
                WireV1.ClusterService.Configuration),
            RuntimeGenerationId =
                "11111111-2222-3333-4444-555555555555",
            ResumeAfterGeneration = 4
        };
        AssertEqual(
            ConfigurationContractValidationError.None,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Subscribe accepts World context");

        request.Context.CallerRole = WireV1.ClusterNodeRole.Login;
        AssertEqual(
            ConfigurationContractValidationError.InvalidCallerRole,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Subscribe rejects Login caller role");
    }

    private static void VerifyGetContract()
    {
        var request = new WireV1.GetConfigurationRequest
        {
            Context = CreateContext(
                WireV1.ClusterNodeRole.World,
                WireV1.ClusterService.Configuration)
        };
        AssertEqual(
            ConfigurationContractValidationError.None,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Get accepts World context");

        request.Context.CallerRole = WireV1.ClusterNodeRole.Login;
        AssertEqual(
            ConfigurationContractValidationError.InvalidCallerRole,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Get rejects Login caller role");

        request.Context = CreateContext(
            WireV1.ClusterNodeRole.World,
            WireV1.ClusterService.Communication);
        AssertEqual(
            ConfigurationContractValidationError.InvalidContext,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Get rejects wrong requested service");
    }

    private static void VerifyUpdateContract()
    {
        var request = new WireV1.UpdateConfigurationRequest
        {
            Context = CreateContext(
                WireV1.ClusterNodeRole.World,
                WireV1.ClusterService.Configuration),
            Configuration = CreateValidSnapshot()
        };
        AssertEqual(
            ConfigurationContractValidationError.None,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Update accepts bounded snapshot");

        request.Configuration = null;
        AssertEqual(
            ConfigurationContractValidationError.MissingConfiguration,
            ClusterConfigurationContractValidator.Validate(request),
            "Configuration Update rejects missing snapshot");
    }

    private static void VerifySnapshotBounds()
    {
        WireV1.ConfigurationSnapshot snapshot = CreateValidSnapshot();
        snapshot.MaxGold = 0;
        AssertEqual(
            ConfigurationContractValidationError.InvalidMaxGold,
            ClusterConfigurationContractValidator.ValidateSnapshot(snapshot),
            "Configuration snapshot rejects non-positive MaxGold");

        snapshot = CreateValidSnapshot();
        snapshot.TimeExpBuffUnixTimeMs =
            ConfigurationContractLimits.MinimumDateTimeUnixMilliseconds - 1;
        AssertEqual(
            ConfigurationContractValidationError.InvalidExpBuffTimestamp,
            ClusterConfigurationContractValidator.ValidateSnapshot(snapshot),
            "Configuration snapshot rejects exp-buff timestamp below DateTime range");

        snapshot = CreateValidSnapshot();
        snapshot.TimeGoldBuffUnixTimeMs =
            ConfigurationContractLimits.MaximumDateTimeUnixMilliseconds + 1;
        AssertEqual(
            ConfigurationContractValidationError.InvalidGoldBuffTimestamp,
            ClusterConfigurationContractValidator.ValidateSnapshot(snapshot),
            "Configuration snapshot rejects gold-buff timestamp above DateTime range");
    }

    private static WireV1.ConfigurationSnapshot CreateValidSnapshot()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new WireV1.ConfigurationSnapshot
        {
            MaxGold = 2_000_000_000L,
            TimeExpBuffUnixTimeMs = now.AddHours(-2).ToUnixTimeMilliseconds(),
            TimeGoldBuffUnixTimeMs = now.AddHours(-2).ToUnixTimeMilliseconds()
        };
    }

    private static WireV1.RequestContext CreateContext(
        WireV1.ClusterNodeRole role,
        WireV1.ClusterService service)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new WireV1.RequestContext
        {
            Version = new WireV1.ProtocolVersion
            {
                Major = ClusterContractVersion.CurrentMajor,
                Minor = ClusterContractVersion.CurrentMinor
            },
            RequestId = Guid.NewGuid().ToString("D"),
            IssuedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
            DeadlineUnixTimeMs = now.AddSeconds(10).ToUnixTimeMilliseconds(),
            CallerRole = role,
            RequestedService = service,
            CallerInstanceId = "configuration-contract-self-test"
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
