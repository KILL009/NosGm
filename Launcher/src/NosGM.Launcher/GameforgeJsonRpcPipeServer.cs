// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace NosGM.Launcher;

internal sealed class GameforgeJsonRpcPipeServer
{
    private const string PipeName = "GameforgeClientJSONRPC";
    private const int MaximumRequestBytes = 16 * 1024;
    private const int MaximumRequests = 8;

    private readonly string _accountName;
    private string? _authorizationCode;
    private readonly Guid _sessionId;
    private bool _authorizationCodeDelivered;
    private bool _accountNameDelivered;

    public GameforgeJsonRpcPipeServer(
        string accountName,
        string authorizationCode,
        Guid sessionId)
    {
        _accountName = accountName;
        _authorizationCode = authorizationCode;
        _sessionId = sessionId;
    }

    public async Task RunAsync(Process process, CancellationToken cancellationToken)
    {
        using var processExited = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            processExited.Token);

        void OnProcessExited(object? sender, EventArgs args) => processExited.Cancel();
        process.EnableRaisingEvents = true;
        process.Exited += OnProcessExited;
        try
        {
            for (var requestIndex = 0;
                 requestIndex < MaximumRequests && !_accountNameDelivered;
                 requestIndex++)
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    4,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await pipe.WaitForConnectionAsync(linked.Token);
                using var request = await ReadJsonRequestAsync(pipe, linked.Token);
                var response = CreateResponse(request.RootElement);
                await pipe.WriteAsync(response, linked.Token);
                await pipe.FlushAsync(linked.Token);
            }
        }
        finally
        {
            process.Exited -= OnProcessExited;
        }

        if (!_authorizationCodeDelivered || !_accountNameDelivered)
        {
            throw new InvalidOperationException(
                "The game client did not complete the Gameforge JSON-RPC authentication handshake.");
        }
    }

    private byte[] CreateResponse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("jsonrpc", out var jsonRpcElement) ||
            jsonRpcElement.GetString() != "2.0" ||
            !root.TryGetProperty("id", out var idElement) ||
            !root.TryGetProperty("method", out var methodElement) ||
            methodElement.ValueKind != JsonValueKind.String)
        {
            return CreateErrorResponse(idElement: null, -32600, "Invalid Request");
        }

        var method = methodElement.GetString();
        return method switch
        {
            "ClientLibrary.isClientRunning" => CreateResultResponse(idElement, writer => writer.WriteBooleanValue(true)),
            "ClientLibrary.initSession" => CreateInitSessionResponse(root, idElement),
            "ClientLibrary.queryAuthorizationCode" => CreateAuthorizationCodeResponse(idElement),
            "ClientLibrary.queryGameAccountName" => CreateAccountNameResponse(idElement),
            _ => CreateErrorResponse(idElement, -32601, "Method not found")
        };
    }

    private byte[] CreateInitSessionResponse(JsonElement root, JsonElement idElement)
    {
        if (!root.TryGetProperty("params", out var paramsElement) ||
            !paramsElement.TryGetProperty("sessionId", out var sessionElement) ||
            sessionElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(sessionElement.GetString(), out var receivedSessionId) ||
            receivedSessionId != _sessionId)
        {
            return CreateErrorResponse(idElement, -32602, "Invalid session");
        }

        return CreateResultResponse(
            idElement,
            writer => writer.WriteStringValue(receivedSessionId.ToString("D")));
    }

    private byte[] CreateAuthorizationCodeResponse(JsonElement idElement)
    {
        if (_authorizationCode is null)
        {
            return CreateErrorResponse(idElement, -32001, "Authorization code already consumed");
        }

        var authorizationCode = _authorizationCode;
        _authorizationCode = null;
        _authorizationCodeDelivered = true;
        return CreateResultResponse(idElement, writer => writer.WriteStringValue(authorizationCode));
    }

    private byte[] CreateAccountNameResponse(JsonElement idElement)
    {
        _accountNameDelivered = true;
        return CreateResultResponse(idElement, writer => writer.WriteStringValue(_accountName));
    }

    private static byte[] CreateResultResponse(
        JsonElement idElement,
        Action<Utf8JsonWriter> writeResult)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");
        writer.WritePropertyName("id");
        idElement.WriteTo(writer);
        writer.WritePropertyName("result");
        writeResult(writer);
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreateErrorResponse(
        JsonElement? idElement,
        int code,
        string message)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");
        writer.WritePropertyName("id");
        if (idElement.HasValue)
        {
            idElement.Value.WriteTo(writer);
        }
        else
        {
            writer.WriteNullValue();
        }
        writer.WriteStartObject("error");
        writer.WriteNumber("code", code);
        writer.WriteString("message", message);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static async Task<JsonDocument> ReadJsonRequestAsync(
        NamedPipeServerStream pipe,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[2048];
        while (memory.Length < MaximumRequestBytes)
        {
            var read = await pipe.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            try
            {
                return JsonDocument.Parse(memory.ToArray(), new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 16
                });
            }
            catch (JsonException) when (memory.Length < MaximumRequestBytes)
            {
                // The pipe uses byte mode, so one JSON request may arrive in several reads.
            }
        }

        throw new InvalidDataException("The Gameforge JSON-RPC request is invalid or too large.");
    }
}
