using System.Runtime.CompilerServices;
using NosGm.Communication.Client;

internal static class CommunicationCallbackMirrorOptionsSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        CommunicationCallbackMirrorOptions defaults =
            CommunicationCallbackMirrorOptions.Load(_ => null);
        AssertEqual(
            false,
            defaults.Enabled,
            "Master callback mirror is disabled by default");
        AssertEqual(
            CommunicationCallbackMirrorOptions.DefaultQueueCapacity,
            defaults.QueueCapacity,
            "Master callback mirror uses the bounded default queue");
        AssertEqual(
            CommunicationCallbackMirrorOptions.DefaultStopTimeoutMilliseconds,
            defaults.StopTimeoutMilliseconds,
            "Master callback mirror uses the bounded default shutdown wait");

        CommunicationCallbackMirrorOptions enabled =
            CommunicationCallbackMirrorOptions.Load(
                name => name switch
                {
                    CommunicationCallbackMirrorOptions.EnabledVariable =>
                        "true",
                    CommunicationCallbackMirrorOptions.QueueCapacityVariable =>
                        "1024",
                    CommunicationCallbackMirrorOptions.StopTimeoutVariable =>
                        "8000",
                    _ => null
                });
        AssertEqual(
            true,
            enabled.Enabled,
            "Explicit callback mirror activation is accepted");
        AssertEqual(
            1024,
            enabled.QueueCapacity,
            "Explicit callback mirror queue capacity is accepted");
        AssertEqual(
            8000,
            enabled.StopTimeoutMilliseconds,
            "Explicit callback mirror shutdown wait is accepted");

        AssertThrows(
            () => CommunicationCallbackMirrorOptions.Load(
                name => name ==
                        CommunicationCallbackMirrorOptions.EnabledVariable
                    ? " true"
                    : null),
            "Callback mirror activation rejects surrounding whitespace");
        AssertThrows(
            () => CommunicationCallbackMirrorOptions.Load(
                name => name ==
                        CommunicationCallbackMirrorOptions.QueueCapacityVariable
                    ? "63"
                    : null),
            "Callback mirror queue rejects values below the safe floor");
        AssertThrows(
            () => CommunicationCallbackMirrorOptions.Load(
                name => name ==
                        CommunicationCallbackMirrorOptions.StopTimeoutVariable
                    ? "30001"
                    : null),
            "Callback mirror shutdown wait rejects values above the ceiling");
    }

    private static void AssertThrows(Action action, string name)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("[PASS] " + name);
            return;
        }

        throw new InvalidOperationException(name + ": no exception was thrown.");
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
