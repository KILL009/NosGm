// SPDX-License-Identifier: MIT

using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using NosGM.Launcher;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("The Gameforge named-pipe self-test requires Windows.");
}

await RunPersistentConnectionScenarioAsync();
await RunReconnectScenarioAsync();
Console.WriteLine("Gameforge JSON-RPC pipe compatibility self-test passed.");

static async Task RunPersistentConnectionScenarioAsync()
{
    var sessionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    const string accountName = "PipeTestAccount";
    const string authorizationCode = "pipe-test-code";

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var server = new GameforgeJsonRpcPipeServer(accountName, authorizationCode, sessionId);
    var serverTask = server.RunAsync(timeout.Token);

    await using var client = CreateClient();
    await client.ConnectAsync(timeout.Token);

    using (var running = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 1,
                   method = "ClientLibrary.isClientRunning"
               },
               timeout.Token))
    {
        Require(running.RootElement.GetProperty("result").ValueKind == JsonValueKind.True,
            "isClientRunning must succeed without params and return JSON true.");
    }

    using (var invalidSession = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 2,
                   method = "ClientLibrary.queryAuthorizationCode",
                   @params = new { sessionId = Guid.Empty.ToString("D") }
               },
               timeout.Token))
    {
        Require(
            invalidSession.RootElement.GetProperty("error").GetProperty("code").GetInt32() == -32602,
            "Sensitive methods must reject an unexpected session.");
    }

    using (var initialized = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 3,
                   method = "ClientLibrary.initSession",
                   @params = new { sessionId = sessionId.ToString("D") }
               },
               timeout.Token))
    {
        Require(
            initialized.RootElement.GetProperty("result").GetString() == sessionId.ToString("D"),
            "initSession must echo the launcher session ID.");
    }

    using (var code = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 4,
                   method = "ClientLibrary.queryAuthorizationCode",
                   @params = new { sessionId = sessionId.ToString("D") }
               },
               timeout.Token))
    {
        Require(
            code.RootElement.GetProperty("result").GetString() == authorizationCode,
            "The authorization code response changed unexpectedly.");
    }

    using (var replay = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 5,
                   method = "ClientLibrary.queryAuthorizationCode",
                   @params = new { sessionId = sessionId.ToString("D") }
               },
               timeout.Token))
    {
        Require(
            replay.RootElement.GetProperty("error").GetProperty("code").GetInt32() == -32001,
            "The authorization code must remain one-use.");
    }

    using (var account = await SendAsync(
               client,
               new
               {
                   jsonrpc = "2.0",
                   id = 6,
                   method = "ClientLibrary.queryGameAccountName",
                   @params = new { sessionId = sessionId.ToString("D") }
               },
               timeout.Token))
    {
        Require(
            account.RootElement.GetProperty("result").GetString() == accountName,
            "The game account name response changed unexpectedly.");
    }

    await serverTask;
}

static async Task RunReconnectScenarioAsync()
{
    var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    const string accountName = "ReconnectAccount";
    const string authorizationCode = "reconnect-code";

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var server = new GameforgeJsonRpcPipeServer(accountName, authorizationCode, sessionId);
    var serverTask = server.RunAsync(timeout.Token);

    await SendOnNewConnectionAsync(
        new
        {
            jsonrpc = "2.0",
            id = 10,
            method = "ClientLibrary.isClientRunning"
        },
        element => element.GetProperty("result").ValueKind == JsonValueKind.True,
        timeout.Token);

    await SendOnNewConnectionAsync(
        new
        {
            jsonrpc = "2.0",
            id = 11,
            method = "ClientLibrary.initSession",
            @params = new { sessionId = sessionId.ToString("D") }
        },
        element => element.GetProperty("result").GetString() == sessionId.ToString("D"),
        timeout.Token);

    await SendOnNewConnectionAsync(
        new
        {
            jsonrpc = "2.0",
            id = 12,
            method = "ClientLibrary.queryAuthorizationCode",
            @params = new { sessionId = sessionId.ToString("D") }
        },
        element => element.GetProperty("result").GetString() == authorizationCode,
        timeout.Token);

    await SendOnNewConnectionAsync(
        new
        {
            jsonrpc = "2.0",
            id = 13,
            method = "ClientLibrary.queryGameAccountName",
            @params = new { sessionId = sessionId.ToString("D") }
        },
        element => element.GetProperty("result").GetString() == accountName,
        timeout.Token);

    await serverTask;
}

static NamedPipeClientStream CreateClient()
{
    return new NamedPipeClientStream(
        ".",
        "GameforgeClientJSONRPC",
        PipeDirection.InOut,
        PipeOptions.Asynchronous);
}

static async Task SendOnNewConnectionAsync(
    object request,
    Func<JsonElement, bool> assertion,
    CancellationToken cancellationToken)
{
    await using var client = CreateClient();
    await client.ConnectAsync(cancellationToken);
    using var response = await SendAsync(client, request, cancellationToken);
    Require(assertion(response.RootElement), "Reconnect-style Gameforge request failed.");
}

static async Task<JsonDocument> SendAsync(
    NamedPipeClientStream client,
    object request,
    CancellationToken cancellationToken)
{
    var requestBytes = JsonSerializer.SerializeToUtf8Bytes(request);
    await client.WriteAsync(requestBytes, cancellationToken);
    await client.FlushAsync(cancellationToken);
    return await ReadJsonAsync(client, cancellationToken);
}

static async Task<JsonDocument> ReadJsonAsync(
    NamedPipeClientStream client,
    CancellationToken cancellationToken)
{
    using var memory = new MemoryStream();
    var buffer = new byte[1024];
    while (memory.Length < 16 * 1024)
    {
        var read = await client.ReadAsync(buffer.AsMemory(), cancellationToken);
        if (read == 0)
        {
            break;
        }

        await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        try
        {
            return JsonDocument.Parse(memory.ToArray());
        }
        catch (JsonException) when (memory.Length < 16 * 1024)
        {
            // Byte-mode named pipes may split one JSON response across reads.
        }
    }

    throw new InvalidDataException("The pipe self-test received an invalid JSON response.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
