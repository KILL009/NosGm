using System.Text.Json;
using NosGm.Authentication.Client.Configuration;
using WireV1 = global::NosGm.Cluster.Wire.V1;

if (args.Length < 1 || args.Length > 2 ||
    (args[0] != "status" && args[0] != "restart"))
{
    Console.Error.WriteLine(
        "Usage: NosGM.ConfigurationRuntimeController status | restart [expected-runtime-generation-id]");
    return 64;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    using var client = new ConfigurationRuntimeControllerClient(
        ConfigurationRuntimeControllerIdentityOptions.Load());
    WireV1.GetConfigurationRuntimeInfoResponse status =
        await client.GetStatusAsync(cancellation.Token);
    if (status.Result != WireV1.ConfigurationResultCode.Success)
    {
        WriteJson(new
        {
            schemaVersion = 1,
            operation = "status",
            result = status.Result.ToString()
        });
        return 2;
    }

    if (args[0] == "status")
    {
        WriteJson(StatusPayload(status));
        return 0;
    }

    string expected = args.Length == 2
        ? args[1]
        : status.RuntimeGenerationId;
    if (!string.Equals(
            expected,
            status.RuntimeGenerationId,
            StringComparison.Ordinal))
    {
        Console.Error.WriteLine(
            "The supplied expected runtime generation does not match the current status.");
        return 3;
    }
    if (!status.ControlEnabled)
    {
        Console.Error.WriteLine(
            "Configuration runtime control is disabled in Authentication.");
        return 4;
    }
    if (!status.Seeded)
    {
        Console.Error.WriteLine(
            "Configuration runtime is not seeded and cannot restart safely.");
        return 5;
    }

    WireV1.RestartConfigurationRuntimeResponse restarted =
        await client.RestartAsync(expected, cancellation.Token);
    WriteJson(new
    {
        schemaVersion = 1,
        operation = "restart",
        result = restarted.Result.ToString(),
        previousRuntimeGenerationId =
            restarted.PreviousRuntimeGenerationId,
        runtimeGenerationId = restarted.RuntimeGenerationId,
        startedAtUnixTimeMs = restarted.StartedAtUnixTimeMs,
        configurationGeneration = restarted.ConfigurationGeneration,
        activeSubscribers = restarted.ActiveSubscribers,
        restartCount = restarted.RestartCount,
        controlEnabled = restarted.ControlEnabled
    });
    return restarted.Result == WireV1.ConfigurationResultCode.Success
        ? 0
        : 6;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Configuration runtime control was cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "Configuration runtime control failed: " +
        exception.GetType().Name + ": " + exception.Message);
    return 1;
}

static object StatusPayload(
    WireV1.GetConfigurationRuntimeInfoResponse status)
{
    return new
    {
        schemaVersion = 1,
        operation = "status",
        result = status.Result.ToString(),
        runtimeGenerationId = status.RuntimeGenerationId,
        startedAtUnixTimeMs = status.StartedAtUnixTimeMs,
        configurationGeneration = status.ConfigurationGeneration,
        seeded = status.Seeded,
        activeSubscribers = status.ActiveSubscribers,
        restartCount = status.RestartCount,
        controlEnabled = status.ControlEnabled
    };
}

static void WriteJson(object value)
{
    Console.WriteLine(
        JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }));
}
