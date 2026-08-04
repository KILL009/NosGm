// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Launcher;

internal sealed record DiscordPresenceActivity(
    string Details,
    string State,
    long StartedAtUnixSeconds,
    string LargeImageKey,
    string LargeImageText,
    string? SmallImageKey = null,
    string? SmallImageText = null,
    string? PartyId = null,
    int PartyCurrent = 0,
    int PartyMaximum = 0);

/// <summary>
/// Minimal Discord desktop RPC client for Rich Presence. It communicates only
/// with the current user's local Discord named pipe and does not authenticate a
/// Discord account or persist any Discord credential.
/// </summary>
internal sealed class DiscordRichPresenceClient : IAsyncDisposable
{
    private const int RpcVersion = 1;
    private const int HandshakeOpcode = 0;
    private const int FrameOpcode = 1;
    private const int MaximumPayloadBytes = 64 * 1024;
    private const int MaximumTextLength = 128;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _applicationId;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();

    private NamedPipeClientStream? _pipe;
    private Task? _readerTask;
    private bool _disposed;

    public DiscordRichPresenceClient(string applicationId)
    {
        _applicationId = applicationId;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_applicationId);

    public async Task<bool> UpdateAsync(
        DiscordPresenceActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (_disposed || !IsConfigured)
        {
            return false;
        }

        var normalized = Normalize(activity);
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                cmd = "SET_ACTIVITY",
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = new
                    {
                        details = normalized.Details,
                        state = normalized.State,
                        timestamps = normalized.StartedAtUnixSeconds > 0
                            ? new { start = normalized.StartedAtUnixSeconds }
                            : null,
                        assets = new
                        {
                            large_image = normalized.LargeImageKey,
                            large_text = normalized.LargeImageText,
                            small_image = normalized.SmallImageKey,
                            small_text = normalized.SmallImageText
                        },
                        party = normalized.PartyMaximum > 0 &&
                                normalized.PartyCurrent > 0 &&
                                !string.IsNullOrWhiteSpace(normalized.PartyId)
                            ? new
                            {
                                id = normalized.PartyId,
                                size = new[]
                                {
                                    normalized.PartyCurrent,
                                    normalized.PartyMaximum
                                }
                            }
                            : null,
                        instance = true
                    }
                },
                nonce = Guid.NewGuid().ToString("N")
            },
            JsonOptions);

        return await SendWithReconnectAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !IsConfigured)
        {
            return;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                cmd = "SET_ACTIVITY",
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = (object?)null
                },
                nonce = Guid.NewGuid().ToString("N")
            },
            JsonOptions);

        _ = await SendWithReconnectAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> SendWithReconnectAsync(
        byte[] payload,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetime.Token);

        if (!await EnsureConnectedAsync(linked.Token).ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            await WriteFrameAsync(FrameOpcode, payload, linked.Token)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Disconnect();
        }

        if (!await EnsureConnectedAsync(linked.Token).ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            await WriteFrameAsync(FrameOpcode, payload, linked.Token)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Disconnect();
            return false;
        }
    }

    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_pipe?.IsConnected == true)
        {
            return true;
        }

        await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_pipe?.IsConnected == true)
            {
                return true;
            }

            Disconnect();
            for (var index = 0; index < 10; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = new NamedPipeClientStream(
                    ".",
                    $"discord-ipc-{index}",
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);
                try
                {
                    using var connectTimeout =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    connectTimeout.CancelAfter(TimeSpan.FromMilliseconds(350));
                    await candidate.ConnectAsync(connectTimeout.Token).ConfigureAwait(false);

                    var handshake = JsonSerializer.SerializeToUtf8Bytes(
                        new
                        {
                            v = RpcVersion,
                            client_id = _applicationId
                        },
                        JsonOptions);
                    await WriteFrameToStreamAsync(
                            candidate,
                            HandshakeOpcode,
                            handshake,
                            cancellationToken)
                        .ConfigureAwait(false);

                    using var readyTimeout =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    readyTimeout.CancelAfter(TimeSpan.FromSeconds(3));
                    _ = await ReadFrameAsync(candidate, readyTimeout.Token)
                        .ConfigureAwait(false);

                    _pipe = candidate;
                    _readerTask = Task.Run(
                        () => ReadLoopAsync(candidate, _lifetime.Token),
                        CancellationToken.None);
                    return true;
                }
                catch (Exception exception) when (
                    exception is IOException or OperationCanceledException or UnauthorizedAccessException)
                {
                    candidate.Dispose();
                }
            }

            return false;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private async Task WriteFrameAsync(
        int opcode,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var pipe = _pipe;
        if (pipe?.IsConnected != true)
        {
            throw new InvalidOperationException("Discord RPC is not connected.");
        }

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteFrameToStreamAsync(pipe, opcode, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static async Task WriteFrameToStreamAsync(
        Stream stream,
        int opcode,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        if (payload.Length <= 0 || payload.Length > MaximumPayloadBytes)
        {
            throw new InvalidDataException("Discord RPC payload size is invalid.");
        }

        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), opcode);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), payload.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(int Opcode, byte[] Payload)> ReadFrameAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var opcode = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        if (payloadLength <= 0 || payloadLength > MaximumPayloadBytes)
        {
            throw new InvalidDataException("Discord RPC returned an invalid frame size.");
        }

        var payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return (opcode, payload);
    }

    private async Task ReadLoopAsync(
        NamedPipeClientStream pipe,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && pipe.IsConnected)
            {
                _ = await ReadFrameAsync(pipe, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (
            exception is IOException or OperationCanceledException or ObjectDisposedException or InvalidDataException)
        {
            // The next presence update reconnects to Discord automatically.
        }
        finally
        {
            if (ReferenceEquals(_pipe, pipe))
            {
                Disconnect();
            }
        }
    }

    private static DiscordPresenceActivity Normalize(DiscordPresenceActivity activity)
    {
        return activity with
        {
            Details = Limit(activity.Details, "Jugando en NosGM"),
            State = Limit(activity.State, "Aventurándose por Sumeria"),
            LargeImageKey = NormalizeAssetKey(activity.LargeImageKey, "nosgm"),
            LargeImageText = Limit(activity.LargeImageText, "NosGM"),
            SmallImageKey = string.IsNullOrWhiteSpace(activity.SmallImageKey)
                ? null
                : NormalizeAssetKey(activity.SmallImageKey, "nosgm"),
            SmallImageText = string.IsNullOrWhiteSpace(activity.SmallImageText)
                ? null
                : Limit(activity.SmallImageText, "NosGM"),
            PartyCurrent = Math.Max(0, activity.PartyCurrent),
            PartyMaximum = Math.Max(0, activity.PartyMaximum)
        };
    }

    private static string Limit(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
        return normalized.Length <= MaximumTextLength
            ? normalized
            : normalized[..MaximumTextLength];
    }

    private static string NormalizeAssetKey(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();
        return normalized.Length <= MaximumTextLength
            ? normalized
            : normalized[..MaximumTextLength];
    }

    private void Disconnect()
    {
        var pipe = Interlocked.Exchange(ref _pipe, null);
        if (pipe is null)
        {
            return;
        }

        try
        {
            pipe.Dispose();
        }
        catch
        {
            // Best-effort cleanup after Discord closes or restarts.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await ClearAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Closing the launcher must not be blocked by Discord.
        }

        _disposed = true;
        _lifetime.Cancel();
        Disconnect();
        if (_readerTask is not null)
        {
            try
            {
                await _readerTask.ConfigureAwait(false);
            }
            catch
            {
                // The reader normally ends through cancellation or pipe closure.
            }
        }

        _lifetime.Dispose();
        _connectionGate.Dispose();
        _writeGate.Dispose();
    }
}
