using System.Runtime.CompilerServices;
using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;

internal static class ClusterConfigurationTransportLiveSelfTest
{
    private const string LiveWorldCertificatePath =
        "NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PATH";
    private const string LiveWorldCertificatePassword =
        "NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PASSWORD";
    private const string LiveLoginCertificatePath =
        "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH";
    private const string LiveLoginCertificatePassword =
        "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PASSWORD";

    [ModuleInitializer]
    public static void Run()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(
                    LiveWorldCertificatePath)))
        {
            return;
        }

        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        AuthenticationGrpcClientOptions worldOptions =
            LoadLiveOptions(
                ClusterNodeRole.World,
                "World",
                LiveWorldCertificatePath,
                LiveWorldCertificatePassword);
        AuthenticationGrpcClientOptions loginOptions =
            LoadLiveOptions(
                ClusterNodeRole.Login,
                "Login",
                LiveLoginCertificatePath,
                LiveLoginCertificatePassword);

        AssertThrows<InvalidOperationException>(
            () => new GrpcClusterConfigurationTransport(loginOptions),
            "Configuration transport rejects the Login certificate role");

        using var transport =
            new GrpcClusterConfigurationTransport(worldOptions);
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(20));

        ConfigurationTransportResult before =
            await transport.GetAsync(timeout.Token);
        if (before.Result != ConfigurationTransportResultCode.Unavailable &&
            before.Result != ConfigurationTransportResultCode.Success)
        {
            throw new InvalidOperationException(
                "Configuration live baseline returned unexpected result '" +
                before.Result + "'.");
        }

        ulong baselineGeneration = before.Generation;
        if (before.Result == ConfigurationTransportResultCode.Unavailable)
        {
            AssertEqual(
                0UL,
                baselineGeneration,
                "Fresh Configuration shadow host starts at generation zero");
        }

        long marker = worldOptions.WireMode ==
            AuthenticationGrpcWireMode.GrpcWeb
                ? 2_100_000_001L
                : 2_100_000_002L;
        var expected = new ConfigurationTransportSnapshot
        {
            MaxGold = marker,
            TimeExpBuffUnixTimeMilliseconds = 1_700_000_100_000L,
            TimeGoldBuffUnixTimeMilliseconds = 1_700_000_200_000L
        };

        ConfigurationTransportResult updated =
            await transport.UpdateAsync(expected, timeout.Token);
        AssertEqual(
            ConfigurationTransportResultCode.Success,
            updated.Result,
            "Live World Configuration update succeeds through mTLS");
        AssertEqual(
            checked(baselineGeneration + 1UL),
            updated.Generation,
            "Live Configuration update advances generation once");
        AssertSnapshot(
            expected,
            updated.Configuration,
            "Live Configuration update echoes the accepted snapshot");

        ConfigurationTransportResult reread =
            await transport.GetAsync(timeout.Token);
        AssertEqual(
            ConfigurationTransportResultCode.Success,
            reread.Result,
            "Live World Configuration get succeeds after shadow seed");
        AssertEqual(
            updated.Generation,
            reread.Generation,
            "Live Configuration read preserves generation");
        AssertSnapshot(
            expected,
            reread.Configuration,
            "Live Configuration read returns the stored snapshot");

        Console.WriteLine(
            "[PASS] Cluster Configuration transport live mTLS self-test");
    }

    private static AuthenticationGrpcClientOptions LoadLiveOptions(
        ClusterNodeRole role,
        string roleName,
        string certificatePathVariable,
        string certificatePasswordVariable)
    {
        return AuthenticationGrpcClientOptions.Load(
            role,
            variableName => variableName switch
            {
                AuthenticationGrpcClientOptions.AddressVariable =>
                    Environment.GetEnvironmentVariable(
                        AuthenticationGrpcClientOptions.AddressVariable),
                AuthenticationGrpcClientOptions.CertificatePathVariable =>
                    ReadRequiredEnvironment(certificatePathVariable),
                AuthenticationGrpcClientOptions.CertificatePasswordVariable =>
                    Environment.GetEnvironmentVariable(
                        certificatePasswordVariable) ?? string.Empty,
                AuthenticationGrpcClientOptions
                        .TrustedRootCertificatePathVariable =>
                    Environment.GetEnvironmentVariable(
                        AuthenticationGrpcClientOptions
                            .TrustedRootCertificatePathVariable),
                AuthenticationGrpcClientOptions.CallerInstanceIdVariable =>
                    "configuration-acceptance-" +
                    roleName.ToLowerInvariant() + "-1",
                AuthenticationGrpcClientOptions.DeadlineVariable => "10000",
                AuthenticationGrpcClientOptions.WireModeVariable =>
                    Environment.GetEnvironmentVariable(
                        AuthenticationGrpcClientOptions.WireModeVariable),
                _ => null
            });
    }

    private static string ReadRequiredEnvironment(string variableName)
    {
        string value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Configuration live acceptance requires " +
                variableName + ".");
        }
        return value;
    }

    private static void AssertSnapshot(
        ConfigurationTransportSnapshot expected,
        ConfigurationTransportSnapshot actual,
        string name)
    {
        if (actual == null ||
            actual.MaxGold != expected.MaxGold ||
            actual.TimeExpBuffUnixTimeMilliseconds !=
                expected.TimeExpBuffUnixTimeMilliseconds ||
            actual.TimeGoldBuffUnixTimeMilliseconds !=
                expected.TimeGoldBuffUnixTimeMilliseconds)
        {
            throw new InvalidOperationException(
                name + ": snapshot mismatch.");
        }
        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertThrows<TException>(
        Action action,
        string name)
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
            name + ": expected " + typeof(TException).Name + ".");
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
