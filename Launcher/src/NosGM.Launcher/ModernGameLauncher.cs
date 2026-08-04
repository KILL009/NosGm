// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Globalization;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal static class ModernGameLauncher
{
    private const string ClientApplicationId = "d3b2a0c1-f0d0-4888-ae0b-1c5e1febdafb";

    internal static event Action<string, string>? PresenceStageChanged;
    internal static event Action<Process, string>? GameLaunched;

    private static readonly IReadOnlyDictionary<string, byte> RegionByLanguage =
        new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = 0,
            ["de"] = 1,
            ["fr"] = 2,
            ["it"] = 3,
            ["pl"] = 4,
            ["es"] = 5,
            ["cz"] = 6,
            ["ru"] = 7,
            ["jp"] = 8,
            ["cn"] = 9
        };

    private static readonly IReadOnlyDictionary<string, string> SteamLanguageByLauncherLanguage =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "english",
            ["de"] = "german",
            ["fr"] = "french",
            ["it"] = "italian",
            ["pl"] = "polish",
            ["es"] = "spanish",
            ["cz"] = "czech",
            ["ru"] = "russian",
            ["jp"] = "japanese",
            ["cn"] = "schinese"
        };

    public static async Task<Process> LaunchAsync(
        LauncherSettings settings,
        string accountName,
        string password,
        CancellationToken cancellationToken)
    {
        ReportPresence("Preparando NosGM", "Validando la instalación");

        if (!RegionByLanguage.TryGetValue(settings.Language, out var countryId))
        {
            throw new InvalidDataException(
                $"The launcher language '{settings.Language}' has no Gameforge region mapping.");
        }

        var installRoot = Path.GetFullPath(settings.InstallRoot);
        var sourceGamePath = SafePaths.ResolveManagedPath(installRoot, settings.GameExecutable);
        if (!File.Exists(sourceGamePath))
        {
            throw new FileNotFoundException("The configured game executable is not installed.", sourceGamePath);
        }

        var transport = ResolveTransport(settings, installRoot);
        var installationId = GameforgeInstallationId.Resolve();
        var gamePath = sourceGamePath;

        if (transport == ModernAuthenticationTransport.SteamStub)
        {
            ReportPresence("Preparando NosGM", "Preparando el cliente de Steam");
            GameforgeInstallationId.EnsureSteamClientIdentity(installationId);
            var preparation = SteamClientPatcher.Prepare(
                installRoot,
                settings.GameExecutable,
                settings.LoginServerAddress);
            gamePath = preparation.ExecutablePath;
        }

        ReportPresence("Iniciando sesión", "Autenticando la cuenta");
        var authenticationClient = new LauncherAuthenticationClient();
        var ticket = await authenticationClient.RequestTicketAsync(
            settings,
            accountName,
            password,
            countryId,
            installationId,
            cancellationToken);

        ReportPresence("Entrando al mundo", "Iniciando el cliente");
        var process = transport == ModernAuthenticationTransport.SteamStub
            ? await LaunchWithSteamStubAsync(
                gamePath,
                installRoot,
                settings.Language,
                countryId,
                installationId,
                ticket,
                cancellationToken)
            : await LaunchWithGameforgePipeAsync(
                gamePath,
                installRoot,
                countryId,
                ticket,
                cancellationToken);

        ReportPresence("Entrando al mundo", "Seleccionando personaje");
        NotifyGameLaunched(process, ticket.AccountName);
        return process;
    }

    private static void ReportPresence(string details, string state)
    {
        try
        {
            PresenceStageChanged?.Invoke(details, state);
        }
        catch
        {
            // Presence observers cannot interrupt login or client startup.
        }
    }

    private static void NotifyGameLaunched(Process process, string accountName)
    {
        try
        {
            GameLaunched?.Invoke(process, accountName);
        }
        catch
        {
            // Presence observers cannot interrupt a successful client launch.
        }
    }

    private static ModernAuthenticationTransport ResolveTransport(
        LauncherSettings settings,
        string installRoot)
    {
        if (string.Equals(settings.AuthenticationTransport, "steam-stub", StringComparison.OrdinalIgnoreCase))
        {
            return ModernAuthenticationTransport.SteamStub;
        }

        if (string.Equals(settings.AuthenticationTransport, "gameforge-pipe", StringComparison.OrdinalIgnoreCase))
        {
            return ModernAuthenticationTransport.GameforgePipe;
        }

        return SteamClientPatcher.IsSteamInstallation(installRoot)
            ? ModernAuthenticationTransport.SteamStub
            : ModernAuthenticationTransport.GameforgePipe;
    }

    private static async Task<Process> LaunchWithSteamStubAsync(
        string gamePath,
        string installRoot,
        string launcherLanguage,
        byte countryId,
        string installationId,
        LauncherAuthorizationTicket ticket,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(gamePath, installRoot, countryId);
        startInfo.Environment["_NC_AUTH_CODE"] = ticket.AuthorizationCode;
        startInfo.Environment["_NC_INSTALLATION_ID"] = installationId;
        startInfo.Environment["_NC_STEAM_LANGUAGE"] =
            SteamLanguageByLauncherLanguage.TryGetValue(launcherLanguage, out var steamLanguage)
                ? steamLanguage
                : "english";

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Windows did not start the patched Steam game process.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    "The patched Steam game process exited before sending modern authentication.");
            }

            return process;
        }
        catch
        {
            TryTerminate(process);
            throw;
        }
    }

    private static async Task<Process> LaunchWithGameforgePipeAsync(
        string gamePath,
        string installRoot,
        byte countryId,
        LauncherAuthorizationTicket ticket,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var pipeServer = new GameforgeJsonRpcPipeServer(
            ticket.AccountName,
            ticket.AuthorizationCode,
            sessionId);
        using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(Math.Min(45, ticket.ExpiresInSeconds)));

        var startInfo = CreateStartInfo(gamePath, installRoot, countryId);
        startInfo.Environment["_TNT_CLIENT_APPLICATION_ID"] = ClientApplicationId;
        startInfo.Environment["_TNT_SESSION_ID"] = sessionId.ToString("D");

        using var processExited = new CancellationTokenSource();
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        EventHandler onProcessExited = (_, _) => processExited.Cancel();
        process.Exited += onProcessExited;

        var pipeTask = pipeServer.RunAsync(handshakeTimeout.Token);
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Windows did not start the game process.");
            }

            var completed = await Task.WhenAny(
                pipeTask,
                Task.Delay(Timeout.InfiniteTimeSpan, processExited.Token));
            if (completed != pipeTask)
            {
                throw new InvalidOperationException(
                    "The game process exited before completing modern authentication.");
            }

            await pipeTask;
            return process;
        }
        catch
        {
            TryTerminate(process);
            throw;
        }
        finally
        {
            process.Exited -= onProcessExited;
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string gamePath,
        string installRoot,
        byte countryId)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = gamePath,
            WorkingDirectory = installRoot,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("gf");
        startInfo.ArgumentList.Add(countryId.ToString(CultureInfo.InvariantCulture));
        return startInfo;
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup after a failed handshake.
        }
    }

    private enum ModernAuthenticationTransport
    {
        GameforgePipe,
        SteamStub
    }
}
