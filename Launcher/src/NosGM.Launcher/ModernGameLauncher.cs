// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Globalization;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal static class ModernGameLauncher
{
    private const string ClientApplicationId = "d3b2a0c1-f0d0-4888-ae0b-1c5e1febdafb";

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

    public static async Task<Process> LaunchAsync(
        LauncherSettings settings,
        string accountName,
        string password,
        CancellationToken cancellationToken)
    {
        if (!RegionByLanguage.TryGetValue(settings.Language, out var countryId))
        {
            throw new InvalidDataException(
                $"The launcher language '{settings.Language}' has no Gameforge region mapping.");
        }

        var installRoot = Path.GetFullPath(settings.InstallRoot);
        var gamePath = SafePaths.ResolveManagedPath(installRoot, settings.GameExecutable);
        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException("The configured game executable is not installed.", gamePath);
        }

        var authenticationClient = new LauncherAuthenticationClient();
        var ticket = await authenticationClient.RequestTicketAsync(
            settings,
            accountName,
            password,
            countryId,
            cancellationToken);

        var sessionId = Guid.NewGuid();
        var pipeServer = new GameforgeJsonRpcPipeServer(
            ticket.AccountName,
            ticket.AuthorizationCode,
            sessionId);
        using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(Math.Min(45, ticket.ExpiresInSeconds)));

        var startInfo = new ProcessStartInfo
        {
            FileName = gamePath,
            WorkingDirectory = installRoot,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("gf");
        startInfo.ArgumentList.Add(countryId.ToString(CultureInfo.InvariantCulture));
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
}
