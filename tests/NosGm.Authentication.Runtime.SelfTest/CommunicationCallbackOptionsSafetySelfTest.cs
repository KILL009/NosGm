using System.Runtime.CompilerServices;
using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;

internal static class CommunicationCallbackOptionsSafetySelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        string certificatePath = Path.GetFullPath(
            "callback-options-client-self-test.pfx");
        string trustedRootPath = Path.GetFullPath(
            "callback-options-root-self-test.cer");
        string cursorPath = Path.GetFullPath(
            "callback-options-self-test.cursor");
        var values = new Dictionary<string, string>
        {
            [CommunicationCallbackSubscriberOptions.CertificatePathVariable] =
                certificatePath,
            [CommunicationCallbackSubscriberOptions
                    .TrustedRootCertificatePathVariable] =
                trustedRootPath,
            [CommunicationCallbackSubscriberOptions.CursorPathVariable] =
                cursorPath,
            [CommunicationCallbackSubscriberOptions.CallerInstanceIdVariable] =
                "callback-options-safety-self-test"
        };

        CommunicationCallbackSubscriberOptions options =
            Load(values);
        AssertEqual(
            certificatePath,
            options.CertificatePath,
            "Callback client certificate path remains isolated");
        AssertEqual(
            trustedRootPath,
            options.TrustedRootCertificatePath,
            "Callback trusted root path remains isolated");
        AssertEqual(
            cursorPath,
            options.CursorPath,
            "Callback cursor path remains isolated");

        values[CommunicationCallbackSubscriberOptions.CursorPathVariable] =
            certificatePath;
        AssertThrows<InvalidOperationException>(
            () => Load(values),
            "Callback cursor cannot overwrite the client certificate");

        values[CommunicationCallbackSubscriberOptions.CursorPathVariable] =
            trustedRootPath;
        AssertThrows<InvalidOperationException>(
            () => Load(values),
            "Callback cursor cannot overwrite the trusted root");

        values[CommunicationCallbackSubscriberOptions.CursorPathVariable] =
            cursorPath;
        values[CommunicationCallbackSubscriberOptions
                .TrustedRootCertificatePathVariable] = certificatePath;
        AssertThrows<InvalidOperationException>(
            () => Load(values),
            "Callback client certificate and trusted root must be distinct");

        Console.WriteLine(
            "[PASS] Communication callback path isolation self-test");
    }

    private static CommunicationCallbackSubscriberOptions Load(
        Dictionary<string, string> values)
    {
        return CommunicationCallbackSubscriberOptions.Load(
            ClusterNodeRole.Login,
            Guid.Empty,
            0,
            string.Empty,
            name => values.TryGetValue(name, out string value)
                ? value
                : null);
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
