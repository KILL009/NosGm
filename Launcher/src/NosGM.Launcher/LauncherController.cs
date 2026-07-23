// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed class LauncherController
{
    public async Task<(UpdatePlan Plan, UpdateResult? Result)> CheckAndApplyAsync(
        LauncherSettings settings,
        bool apply,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!TrustedChannel.IsConfigured)
        {
            throw new InvalidOperationException(
                "This build has no trusted release channel. Configure TrustedChannel.cs before publishing.");
        }

        var manifest = await DownloadManifestAsync(cancellationToken);
        var verifiedManifest = ManifestSecurity.Verify(
            manifest,
            TrustedChannel.KeyId,
            TrustedChannel.PublicKeyPem);
        EnforceMinimumLauncherVersion(manifest.MinimumLauncherVersion);

        var plan = await UpdatePlanner.CreateAsync(
            settings.InstallRoot,
            verifiedManifest,
            progress,
            cancellationToken);

        if (!apply || (plan.Downloads.Count == 0 && plan.Deletes.Count == 0))
        {
            return (plan, null);
        }

        await using var source = new HttpContentSource(TrustedChannel.ContentBaseUri);
        var updater = new TransactionalUpdater();
        var result = await updater.ApplyAsync(
            settings.InstallRoot,
            plan,
            source,
            progress,
            cancellationToken);
        return (plan, result);
    }

    public static void LaunchGame(LauncherSettings settings)
    {
        var installRoot = Path.GetFullPath(settings.InstallRoot);
        var gamePath = SafePaths.ResolveManagedPath(installRoot, settings.GameExecutable);
        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException("The configured game executable is not installed.", gamePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = gamePath,
            WorkingDirectory = installRoot,
            UseShellExecute = true
        };
        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows did not start the game process.");
    }

    public static void OpenInstallFolder(LauncherSettings settings)
    {
        Directory.CreateDirectory(settings.InstallRoot);
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            ArgumentList = { Path.GetFullPath(settings.InstallRoot) },
            UseShellExecute = false
        });
    }

    private static async Task<ReleaseManifest> DownloadManifestAsync(CancellationToken cancellationToken)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            CheckCertificateRevocationList = true
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        using var response = await client.GetAsync(
            TrustedChannel.ManifestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is long length &&
            (length <= 0 || length > JsonSupport.MaxJsonBytes))
        {
            throw new InvalidDataException("Remote manifest size is invalid.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[32 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + read > JsonSupport.MaxJsonBytes)
            {
                throw new InvalidDataException("Remote manifest exceeded the maximum size.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return ManifestIO.ReadUtf8(memory.ToArray());
    }

    private static void EnforceMinimumLauncherVersion(string minimumVersionText)
    {
        if (!Version.TryParse(minimumVersionText, out var minimumVersion))
        {
            throw new InvalidDataException("Manifest minimum launcher version is invalid.");
        }

        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
        if (currentVersion < minimumVersion)
        {
            throw new InvalidOperationException(
                $"Launcher {minimumVersion} or newer is required. Current version: {currentVersion}.");
        }
    }
}
