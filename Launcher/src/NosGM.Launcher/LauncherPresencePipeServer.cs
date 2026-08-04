// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NosGM.Launcher;

/// <summary>
/// Receives bounded, sanitized gameplay-state snapshots from a local NosGM World
/// process. The pipe is scoped to the current Windows user and its name contains
/// only a SHA-256-derived account route, never the raw account name.
/// </summary>
internal sealed class LauncherPresencePipeServer : IAsyncDisposable
{
    private const int MaximumPayloadBytes = 8 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    private readonly string _pipeName;
    private readonly Func<LauncherPresenceState, Task> _onPresence;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _acceptLoop;

    public LauncherPresencePipeServer(
        string accountName,
        Func<LauncherPresenceState, Task> onPresence)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("A presence account route is required.", nameof(accountName));
        }

        _onPresence = onPresence ?? throw new ArgumentNullException(nameof(onPresence));
        _pipeName = BuildPipeName(accountName);
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_lifetime.Token));
    }

    internal static string BuildPipeName(string accountName)
    {
        var normalized = accountName.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var route = Convert.ToHexString(hash)[..24].ToLowerInvariant();
        return $"nosgm-presence-{route}";
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                4096,
                MaximumPayloadBytes + 4);

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                var header = new byte[4];
                await pipe.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
                var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (payloadLength <= 0 || payloadLength > MaximumPayloadBytes)
                {
                    continue;
                }

                var payload = new byte[payloadLength];
                await pipe.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);

                LauncherPresenceState? state;
                try
                {
                    state = JsonSerializer.Deserialize<LauncherPresenceState>(payload, JsonOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (state is null || state.SchemaVersion != 1)
                {
                    continue;
                }

                await _onPresence(state).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or ObjectDisposedException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        try
        {
            await _acceptLoop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown while waiting for the next World snapshot.
        }
        finally
        {
            _lifetime.Dispose();
        }
    }
}
