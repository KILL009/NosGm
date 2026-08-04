// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace NosGM.Launcher;

public partial class MainWindow
{
    private readonly SemaphoreSlim _presenceLifecycleGate = new(1, 1);
    private DiscordRichPresenceClient? _discordPresenceClient;
    private LauncherPresencePipeServer? _launcherPresencePipeServer;
    private Process? _presenceGameProcess;
    private long _presenceStartedAtUnixSeconds;

    private async Task InitializeDiscordPresenceAsync()
    {
        if (!_settings.DiscordRichPresenceEnabled ||
            string.IsNullOrWhiteSpace(_settings.DiscordApplicationId))
        {
            return;
        }

        await _presenceLifecycleGate.WaitAsync().ConfigureAwait(true);
        try
        {
            _presenceStartedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _discordPresenceClient = new DiscordRichPresenceClient(
                _settings.DiscordApplicationId);

            if (!string.IsNullOrWhiteSpace(_settings.AccountName))
            {
                await ReplacePresencePipeServerAsync(_settings.AccountName)
                    .ConfigureAwait(true);
            }
        }
        finally
        {
            _presenceLifecycleGate.Release();
        }

        await PublishLauncherPresenceStageAsync(
            "Launcher listo",
            "Preparando la próxima aventura").ConfigureAwait(true);
    }

    private async Task PublishLauncherPresenceStageAsync(
        string details,
        string state)
    {
        var client = _discordPresenceClient;
        if (client is null)
        {
            return;
        }

        try
        {
            var activity = new DiscordPresenceActivity(
                details,
                state,
                _presenceStartedAtUnixSeconds > 0
                    ? _presenceStartedAtUnixSeconds
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                "nosgm",
                "NosGM",
                "launcher",
                "NosGM Launcher");
            _ = await client.UpdateAsync(activity).ConfigureAwait(true);
        }
        catch
        {
            // Discord must never block updates, login or launching the game.
        }
    }

    private async Task AttachGamePresenceAsync(
        Process gameProcess,
        string accountName)
    {
        if (_discordPresenceClient is null)
        {
            return;
        }

        await _presenceLifecycleGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (_presenceGameProcess is not null)
            {
                _presenceGameProcess.Exited -= PresenceGameProcess_Exited;
                _presenceGameProcess.Dispose();
            }

            _presenceGameProcess = gameProcess;
            _presenceGameProcess.EnableRaisingEvents = true;
            _presenceGameProcess.Exited += PresenceGameProcess_Exited;
            _presenceStartedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await ReplacePresencePipeServerAsync(accountName).ConfigureAwait(true);
        }
        finally
        {
            _presenceLifecycleGate.Release();
        }

        await PublishLauncherPresenceStageAsync(
            "Entrando al mundo",
            "Esperando el personaje y el mapa").ConfigureAwait(true);
    }

    private async Task ReplacePresencePipeServerAsync(string accountName)
    {
        if (_launcherPresencePipeServer is not null)
        {
            await _launcherPresencePipeServer.DisposeAsync().ConfigureAwait(true);
            _launcherPresencePipeServer = null;
        }

        _launcherPresencePipeServer = new LauncherPresencePipeServer(
            accountName,
            ApplyWorldPresenceAsync);
    }

    private async Task ApplyWorldPresenceAsync(LauncherPresenceState state)
    {
        var client = _discordPresenceClient;
        if (client is null)
        {
            return;
        }

        try
        {
            var activity = state.ToDiscordActivity(
                _settings,
                _presenceStartedAtUnixSeconds);
            _ = await client.UpdateAsync(activity).ConfigureAwait(false);
        }
        catch
        {
            // A malformed or stale presence snapshot cannot affect the game.
        }
    }

    private void PresenceGameProcess_Exited(object? sender, EventArgs e)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            await StopGamePresenceAsync().ConfigureAwait(true);
        });
    }

    private async Task StopGamePresenceAsync()
    {
        await _presenceLifecycleGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (_presenceGameProcess is not null)
            {
                _presenceGameProcess.Exited -= PresenceGameProcess_Exited;
                _presenceGameProcess.Dispose();
                _presenceGameProcess = null;
            }

            if (_launcherPresencePipeServer is not null)
            {
                await _launcherPresencePipeServer.DisposeAsync().ConfigureAwait(true);
                _launcherPresencePipeServer = null;
            }
        }
        finally
        {
            _presenceLifecycleGate.Release();
        }

        if (_discordPresenceClient is not null)
        {
            try
            {
                await _discordPresenceClient.ClearAsync().ConfigureAwait(true);
            }
            catch
            {
                // Discord may already be closed.
            }
        }
    }

    private async Task ShutdownDiscordPresenceAsync()
    {
        await _presenceLifecycleGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (_presenceGameProcess is not null)
            {
                _presenceGameProcess.Exited -= PresenceGameProcess_Exited;
                _presenceGameProcess.Dispose();
                _presenceGameProcess = null;
            }

            if (_launcherPresencePipeServer is not null)
            {
                await _launcherPresencePipeServer.DisposeAsync().ConfigureAwait(true);
                _launcherPresencePipeServer = null;
            }

            if (_discordPresenceClient is not null)
            {
                await _discordPresenceClient.DisposeAsync().ConfigureAwait(true);
                _discordPresenceClient = null;
            }
        }
        catch
        {
            // Launcher shutdown is best-effort and must remain immediate.
        }
        finally
        {
            _presenceLifecycleGate.Release();
            _presenceLifecycleGate.Dispose();
        }
    }
}
