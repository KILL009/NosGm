using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using NosGm.Authentication.Client;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.Security;
using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;
using WireNodeRole = NosGm.Cluster.Wire.V1.ClusterNodeRole;

internal static class MasterCertificateRoleSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        VerifyMasterClientIdentityCanBeLoaded();
        VerifyMasterFingerprintResolvesOnlyToMaster();
        VerifyMasterCertificateCannotBeReusedAcrossRoles();
        VerifyLegacyAuthenticationOnlyConfigurationRemainsValid();

        if (Environment.GetCommandLineArgs()
            .Contains("--live", StringComparer.Ordinal))
        {
            VerifyLiveMasterCertificateIsAuthenticatedButUnauthorized();
        }
    }

    private static void VerifyMasterClientIdentityCanBeLoaded()
    {
        var values = new Dictionary<string, string>
        {
            [MasterCommunicationGrpcIdentityOptions.CertificatePathVariable] =
                Path.GetFullPath("master-certificate-self-test.pfx"),
            [MasterCommunicationGrpcIdentityOptions.CallerInstanceIdVariable] =
                "master-callback-publisher-self-test-1"
        };

        AuthenticationGrpcClientOptions options =
            MasterCommunicationGrpcIdentityOptions.Load(
                name => values.TryGetValue(name, out string value)
                    ? value
                    : null);

        AssertEqual(
            ClusterNodeRole.Master,
            options.CallerRole,
            "Master gRPC callers retain the dedicated Master role");
        AssertEqual(
            Path.GetFullPath("master-certificate-self-test.pfx"),
            options.CertificatePath,
            "Master callback publication uses its separate certificate namespace");
    }

    private static void VerifyMasterFingerprintResolvesOnlyToMaster()
    {
        string masterFingerprint = new('D', 64);
        AuthenticationServerOptions options = LoadServerOptions(
            masterFingerprint);
        var roleMap = new ClientCertificateRoleMap(options);

        AssertEqual(
            true,
            roleMap.TryResolveFingerprint(
                masterFingerprint,
                out WireNodeRole role),
            "Configured Master certificate is recognized");
        AssertEqual(
            WireNodeRole.Master,
            role,
            "Master certificate receives only the Master role");
    }

    private static void VerifyMasterCertificateCannotBeReusedAcrossRoles()
    {
        string reusedFingerprint = new('C', 64);
        var values = CreateServerValues(reusedFingerprint);
        values[AuthenticationServerOptions.WorldFingerprintsVariable] =
            reusedFingerprint;

        AssertThrows<InvalidOperationException>(
            () => AuthenticationServerOptions.Load(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(values)
                    .Build()),
            "Master certificate cannot be reused by World");
    }

    private static void VerifyLegacyAuthenticationOnlyConfigurationRemainsValid()
    {
        var values = CreateServerValues(masterFingerprint: null);
        AuthenticationServerOptions options =
            AuthenticationServerOptions.Load(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(values)
                    .Build());

        AssertEqual(
            0,
            options.AllowedFingerprints[WireNodeRole.Master].Count,
            "Authentication-only deployments may omit Master until callbacks activate");
    }

    private static void VerifyLiveMasterCertificateIsAuthenticatedButUnauthorized()
    {
        using var transport = new GrpcGameforgeAuthenticationTransport(
            LoadLiveMasterOptions());
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            transport.IssueAuthTicketAsync(
                    "master-role-probe",
                    Guid.NewGuid().ToString("D"),
                    Guid.NewGuid().ToString("D"),
                    5,
                    timeout.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (RpcException exception)
            when (exception.StatusCode == StatusCode.PermissionDenied)
        {
            Console.WriteLine(
                "[PASS] Live Master certificate authenticates through mTLS but cannot impersonate AuthBridge");
            return;
        }

        throw new InvalidOperationException(
            "Live Master certificate was not rejected from the AuthBridge-only RPC.");
    }

    private static AuthenticationGrpcClientOptions LoadLiveMasterOptions()
    {
        const string prefix = "NOSGM_AUTH_GRPC_LIVE_MASTER";
        return MasterCommunicationGrpcIdentityOptions.Load(
            variableName => variableName switch
            {
                MasterCommunicationGrpcIdentityOptions.AddressVariable =>
                    ReadRequiredEnvironment(
                        AuthenticationGrpcClientOptions.AddressVariable),
                MasterCommunicationGrpcIdentityOptions.CertificatePathVariable =>
                    ReadRequiredEnvironment(prefix + "_CERT_PATH"),
                MasterCommunicationGrpcIdentityOptions
                        .CertificatePasswordVariable =>
                    Environment.GetEnvironmentVariable(
                        prefix + "_CERT_PASSWORD") ?? string.Empty,
                MasterCommunicationGrpcIdentityOptions
                        .TrustedRootCertificatePathVariable =>
                    Environment.GetEnvironmentVariable(
                        AuthenticationGrpcClientOptions
                            .TrustedRootCertificatePathVariable),
                MasterCommunicationGrpcIdentityOptions.CallerInstanceIdVariable =>
                    "acceptance-master-callback-publisher-1",
                MasterCommunicationGrpcIdentityOptions.DeadlineVariable =>
                    "10000",
                MasterCommunicationGrpcIdentityOptions.WireModeVariable =>
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
                $"Live Master certificate acceptance requires {variableName}.");
        }

        return value;
    }

    private static AuthenticationServerOptions LoadServerOptions(
        string masterFingerprint)
    {
        return AuthenticationServerOptions.Load(
            new ConfigurationBuilder()
                .AddInMemoryCollection(CreateServerValues(masterFingerprint))
                .Build());
    }

    private static Dictionary<string, string> CreateServerValues(
        string masterFingerprint)
    {
        var values = new Dictionary<string, string>
        {
            [AuthenticationServerOptions.CertificatePathVariable] =
                Path.GetFullPath("authentication-master-role-self-test.pfx"),
            [AuthenticationServerOptions.AuthBridgeFingerprintsVariable] =
                new string('A', 64),
            [AuthenticationServerOptions.LoginFingerprintsVariable] =
                new string('B', 64),
            [AuthenticationServerOptions.WorldFingerprintsVariable] =
                new string('C', 64)
        };

        if (!string.IsNullOrEmpty(masterFingerprint))
        {
            values[AuthenticationServerOptions.MasterFingerprintsVariable] =
                masterFingerprint;
        }

        return values;
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
