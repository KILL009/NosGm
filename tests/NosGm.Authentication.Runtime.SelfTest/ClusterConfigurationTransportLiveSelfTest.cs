using System.Runtime.CompilerServices;
using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;

internal static class ClusterConfigurationTransportLiveSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        AssertThrows<ArgumentNullException>(
            () => new GrpcClusterConfigurationTransport(null),
            "Configuration transport rejects missing options");

        string fakeCertificatePath =
            Path.GetFullPath("configuration-login-self-test.pfx");
        AuthenticationGrpcClientOptions loginOptions =
            AuthenticationGrpcClientOptions.Load(
                ClusterNodeRole.Login,
                variableName => variableName switch
                {
                    AuthenticationGrpcClientOptions.CertificatePathVariable =>
                        fakeCertificatePath,
                    AuthenticationGrpcClientOptions.CallerInstanceIdVariable =>
                        "configuration-login-self-test-1",
                    AuthenticationGrpcClientOptions.WireModeVariable =>
                        "GRPCWEB",
                    _ => null
                });

        AssertThrows<InvalidOperationException>(
            () => new GrpcClusterConfigurationTransport(loginOptions),
            "Configuration transport rejects Login before certificate loading");

        Console.WriteLine(
            "[PASS] Cluster Configuration transport construction self-test");
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
}
