// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal enum LauncherDiagnosticStatus
{
    Passed,
    Information,
    Warning,
    Failed
}

internal sealed record LauncherDiagnosticCheck(
    string Id,
    string Title,
    LauncherDiagnosticStatus Status,
    string Summary,
    string SuggestedAction = "",
    string Details = "");

internal sealed record LauncherDiagnosticEnvironment(
    string OperatingSystem,
    string Runtime,
    string LauncherVersion,
    string Language,
    string InstallationRoot,
    string GameExecutable,
    string AuthenticationTransport,
    string LoginServer,
    string Portal);

internal sealed record LauncherDiagnosticReport(
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    LauncherDiagnosticStatus OverallStatus,
    LauncherDiagnosticEnvironment Environment,
    IReadOnlyList<LauncherDiagnosticCheck> Checks);

internal sealed class LauncherDiagnosticsService : IDisposable
{
    private const int MaximumPortalResponseBytes = 256 * 1024;
    private const long LowDiskWarningBytes = 2L * 1024 * 1024 * 1024;
    private const long CriticalDiskBytes = 512L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private bool _disposed;

    public LauncherDiagnosticsService()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All,
            CheckCertificateRevocationList = true,
            ConnectTimeout = TimeSpan.FromSeconds(3)
        };
        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NosGM-Launcher-Diagnostics/1.0");
    }

    public async Task<LauncherDiagnosticReport> RunAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(settings);

        var checks = new List<LauncherDiagnosticCheck>
        {
            CheckInstallationRoot(settings),
            CheckGameExecutable(settings),
            await CheckWriteAccessAsync(settings, cancellationToken).ConfigureAwait(false),
            CheckDiskSpace(settings),
            CheckAuthentication(settings),
            CheckDiscord(settings)
        };

        checks.Add(await CheckPortalAsync(settings, cancellationToken).ConfigureAwait(false));
        checks.Add(await CheckTcpServiceAsync(
            "master",
            "Master Server",
            settings.LoginServerAddress,
            4545,
            cancellationToken).ConfigureAwait(false));
        checks.Add(await CheckTcpServiceAsync(
            "world",
            "World Server",
            settings.LoginServerAddress,
            1337,
            cancellationToken).ConfigureAwait(false));
        checks.Add(await CheckTcpServiceAsync(
            "login",
            "Login ES",
            settings.LoginServerAddress,
            4005,
            cancellationToken).ConfigureAwait(false));

        var overall = checks.Any(check => check.Status == LauncherDiagnosticStatus.Failed)
            ? LauncherDiagnosticStatus.Failed
            : checks.Any(check => check.Status == LauncherDiagnosticStatus.Warning)
                ? LauncherDiagnosticStatus.Warning
                : LauncherDiagnosticStatus.Passed;

        return new LauncherDiagnosticReport(
            1,
            DateTimeOffset.UtcNow,
            overall,
            BuildEnvironment(settings),
            checks);
    }

    public async Task ExportSupportBundleAsync(
        LauncherDiagnosticReport report,
        LauncherSettings settings,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("A diagnostic ZIP destination is required.", nameof(destinationPath));
        }

        var outputPath = Path.GetFullPath(destinationPath);
        if (!string.Equals(Path.GetExtension(outputPath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The support bundle must use the .zip extension.");
        }

        var outputDirectory = Path.GetDirectoryName(outputPath)
                              ?? throw new InvalidDataException("The support bundle destination is invalid.");
        Directory.CreateDirectory(outputDirectory);

        var stagingRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NosGM",
            "Launcher",
            "Diagnostics",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await File.WriteAllTextAsync(
                    Path.Combine(stagingRoot, "launcher-diagnostics.json"),
                    JsonSerializer.Serialize(report, JsonOptions),
                    new UTF8Encoding(false),
                    cancellationToken)
                .ConfigureAwait(false);

            await File.WriteAllTextAsync(
                    Path.Combine(stagingRoot, "launcher-diagnostics.txt"),
                    BuildTextReport(report),
                    new UTF8Encoding(false),
                    cancellationToken)
                .ConfigureAwait(false);

            await File.WriteAllTextAsync(
                    Path.Combine(stagingRoot, "settings-summary.json"),
                    JsonSerializer.Serialize(BuildSafeSettingsSummary(settings), JsonOptions),
                    new UTF8Encoding(false),
                    cancellationToken)
                .ConfigureAwait(false);

            await File.WriteAllTextAsync(
                    Path.Combine(stagingRoot, "privacy.txt"),
                    "This bundle intentionally excludes account names, passwords, authorization codes, " +
                    "tickets, Discord secrets, process environment variables, chat messages, exact game " +
                    "coordinates and complete launcher settings." + Environment.NewLine,
                    new UTF8Encoding(false),
                    cancellationToken)
                .ConfigureAwait(false);

            var fingerprint = await BuildClientFingerprintAsync(settings, cancellationToken)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                    Path.Combine(stagingRoot, "client-fingerprint.json"),
                    JsonSerializer.Serialize(fingerprint, JsonOptions),
                    new UTF8Encoding(false),
                    cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            ZipFile.CreateFromDirectory(
                stagingRoot,
                outputPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, recursive: true);
                }
            }
            catch
            {
                // A diagnostic export must not fail only because temporary cleanup was delayed.
            }
        }
    }

    private static LauncherDiagnosticCheck CheckInstallationRoot(LauncherSettings settings)
    {
        try
        {
            var fullPath = Path.GetFullPath(settings.InstallRoot);
            if (!Directory.Exists(fullPath))
            {
                return Failed(
                    "install-root",
                    "Carpeta de instalación",
                    "La carpeta configurada no existe.",
                    "Selecciona la carpeta correcta o usa Reparar para crear una instalación administrada.");
            }

            var attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                return Failed(
                    "install-root",
                    "Carpeta de instalación",
                    "La carpeta es un enlace o punto de reanálisis no permitido.",
                    "Selecciona una carpeta física normal para proteger las actualizaciones.");
            }

            return Passed(
                "install-root",
                "Carpeta de instalación",
                "La carpeta existe y usa una ruta física válida.",
                SanitizePath(fullPath));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Failed(
                "install-root",
                "Carpeta de instalación",
                "No se pudo inspeccionar la carpeta configurada.",
                "Comprueba la ruta y los permisos de Windows.",
                exception.GetType().Name);
        }
    }

    private static LauncherDiagnosticCheck CheckGameExecutable(LauncherSettings settings)
    {
        try
        {
            var gamePath = SafePaths.ResolveManagedPath(
                settings.InstallRoot,
                settings.GameExecutable);
            if (!File.Exists(gamePath))
            {
                return Failed(
                    "game-client",
                    "Cliente del juego",
                    $"No se encontró {settings.GameExecutable}.",
                    "Pulsa Reparar para recuperar los archivos que faltan.");
            }

            var file = new FileInfo(gamePath);
            if (file.Length <= 0)
            {
                return Failed(
                    "game-client",
                    "Cliente del juego",
                    "El ejecutable está vacío.",
                    "Pulsa Reparar para descargar una copia válida.");
            }

            var version = FileVersionInfo.GetVersionInfo(gamePath).FileVersion;
            return Passed(
                "game-client",
                "Cliente del juego",
                "El ejecutable principal está disponible.",
                $"Archivo={file.Name}; Tamaño={FormatBytes(file.Length)}; Versión={SafeText(version, "no publicada")}");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            return Failed(
                "game-client",
                "Cliente del juego",
                "El ejecutable no superó la validación de ruta segura.",
                "Selecciona una instalación sin enlaces simbólicos y ejecuta Reparar.",
                exception.GetType().Name);
        }
    }

    private static async Task<LauncherDiagnosticCheck> CheckWriteAccessAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(settings.InstallRoot))
        {
            return Warning(
                "write-access",
                "Permisos de actualización",
                "No se pueden probar permisos porque la carpeta no existe.",
                "Crea o selecciona primero una carpeta de instalación.");
        }

        var probePath = Path.Combine(
            settings.InstallRoot,
            $".nosgm-diagnostic-{Guid.NewGuid():N}.tmp");
        try
        {
            await using var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            await stream.WriteAsync([0x4E, 0x47, 0x4D], cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return Passed(
                "write-access",
                "Permisos de actualización",
                "El launcher puede escribir y reemplazar archivos.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return Failed(
                "write-access",
                "Permisos de actualización",
                "Windows impide escribir en la carpeta del cliente.",
                "Mueve el cliente fuera de carpetas protegidas o concede permisos al usuario actual.",
                exception.GetType().Name);
        }
        finally
        {
            try
            {
                File.Delete(probePath);
            }
            catch
            {
                // FileOptions.DeleteOnClose normally removes the probe.
            }
        }
    }

    private static LauncherDiagnosticCheck CheckDiskSpace(LauncherSettings settings)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(settings.InstallRoot));
            if (string.IsNullOrWhiteSpace(root))
            {
                return Warning(
                    "disk-space",
                    "Espacio disponible",
                    "No se pudo identificar la unidad de instalación.",
                    "Comprueba manualmente que la unidad tenga espacio suficiente.");
            }

            var drive = new DriveInfo(root);
            var free = drive.AvailableFreeSpace;
            if (free < CriticalDiskBytes)
            {
                return Failed(
                    "disk-space",
                    "Espacio disponible",
                    $"Solo quedan {FormatBytes(free)} libres.",
                    "Libera al menos 2 GB antes de actualizar o reparar.");
            }

            if (free < LowDiskWarningBytes)
            {
                return Warning(
                    "disk-space",
                    "Espacio disponible",
                    $"Quedan {FormatBytes(free)} libres.",
                    "Conviene liberar al menos 2 GB para futuras actualizaciones.");
            }

            return Passed(
                "disk-space",
                "Espacio disponible",
                $"Hay {FormatBytes(free)} libres en la unidad del cliente.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Warning(
                "disk-space",
                "Espacio disponible",
                "No se pudo leer el espacio libre de la unidad.",
                "Comprueba la unidad desde el Explorador de archivos.",
                exception.GetType().Name);
        }
    }

    private static LauncherDiagnosticCheck CheckAuthentication(LauncherSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AuthenticationEndpoint))
        {
            return Warning(
                "authentication",
                "Autenticación moderna",
                "El endpoint moderno no está configurado; se usará el arranque heredado.",
                "Configura el endpoint oficial de NosGM para usar tickets de un solo uso.");
        }

        if (!Uri.TryCreate(settings.AuthenticationEndpoint, UriKind.Absolute, out var endpoint))
        {
            return Failed(
                "authentication",
                "Autenticación moderna",
                "El endpoint configurado no es una URL válida.",
                "Corrige la configuración del launcher.");
        }

        var protectedTransport = string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                                 || endpoint.IsLoopback;
        return protectedTransport
            ? Passed(
                "authentication",
                "Autenticación moderna",
                $"Configurada mediante {settings.AuthenticationTransport}.",
                SafeEndpoint(endpoint))
            : Failed(
                "authentication",
                "Autenticación moderna",
                "La autenticación remota no usa HTTPS.",
                "Utiliza HTTPS fuera de localhost.");
    }

    private static LauncherDiagnosticCheck CheckDiscord(LauncherSettings settings)
    {
        if (!settings.DiscordRichPresenceEnabled)
        {
            return Information(
                "discord",
                "Discord Rich Presence",
                "La presencia de Discord está desactivada por preferencia.");
        }

        return string.Equals(
            settings.DiscordApplicationId,
            LauncherSettings.OfficialDiscordApplicationId,
            StringComparison.Ordinal)
            ? Passed(
                "discord",
                "Discord Rich Presence",
                "La aplicación oficial de NosGM está configurada.")
            : Warning(
                "discord",
                "Discord Rich Presence",
                "Se está usando una Application ID distinta de la oficial.",
                "Restablece la Application ID oficial si la tarjeta no aparece correctamente.");
    }

    private async Task<LauncherDiagnosticCheck> CheckPortalAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(settings.PortalBaseUri, UriKind.Absolute, out var portalBase))
        {
            return Failed(
                "portal",
                "Portal público",
                "La URL del portal no es válida.",
                "Corrige PortalBaseUri en la configuración.");
        }

        var statusUri = new Uri(portalBase, "api/v1/public/status");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, statusUri);
            using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Warning(
                    "portal",
                    "Portal público",
                    $"El portal respondió HTTP {(int)response.StatusCode}.",
                    "Comprueba que el portal y el snapshot firmado estén activos.");
            }

            var body = await ReadBoundedAsync(
                    response.Content,
                    MaximumPortalResponseBytes,
                    timeout.Token)
                .ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Portal status is not a JSON object.");
            }

            return Passed(
                "portal",
                "Portal público",
                "Noticias, estado y operaciones públicas son accesibles.",
                SafeEndpoint(statusUri));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Warning(
                "portal",
                "Portal público",
                "El portal no respondió dentro de cinco segundos.",
                "Comprueba la conexión y vuelve a intentarlo.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or InvalidDataException or JsonException)
        {
            return Warning(
                "portal",
                "Portal público",
                "No se pudo validar la respuesta pública del portal.",
                "El juego puede abrir, pero el dashboard vivo quedará limitado.",
                exception.GetType().Name);
        }
    }

    private static async Task<LauncherDiagnosticCheck> CheckTcpServiceAsync(
        string id,
        string title,
        string host,
        int port,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(1800));
        try
        {
            using var client = new TcpClient(AddressFamily.InterNetwork);
            var stopwatch = Stopwatch.StartNew();
            await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
            stopwatch.Stop();
            return Passed(
                $"tcp-{id}",
                title,
                $"Conexión disponible en {stopwatch.ElapsedMilliseconds} ms.",
                $"{host}:{port}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Warning(
                $"tcp-{id}",
                title,
                $"No respondió en el puerto {port}.",
                "Comprueba que el servicio esté iniciado y permitido por el firewall.");
        }
        catch (Exception exception) when (
            exception is SocketException or IOException or ArgumentException)
        {
            return Warning(
                $"tcp-{id}",
                title,
                $"No se pudo conectar al puerto {port}.",
                "Comprueba el servicio, la dirección y el firewall.",
                exception.GetType().Name);
        }
    }

    private static LauncherDiagnosticEnvironment BuildEnvironment(LauncherSettings settings)
    {
        var launcherVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                              ?? "unknown";
        return new LauncherDiagnosticEnvironment(
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            launcherVersion,
            settings.Language,
            SanitizePath(settings.InstallRoot),
            settings.GameExecutable,
            settings.AuthenticationTransport,
            settings.LoginServerAddress,
            Uri.TryCreate(settings.PortalBaseUri, UriKind.Absolute, out var portal)
                ? SafeEndpoint(portal)
                : "invalid");
    }

    private static async Task<object> BuildClientFingerprintAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = SafePaths.ResolveManagedPath(settings.InstallRoot, settings.GameExecutable);
            if (!File.Exists(path))
            {
                return new
                {
                    file = settings.GameExecutable,
                    status = "missing"
                };
            }

            var info = new FileInfo(path);
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return new
            {
                file = info.Name,
                status = "available",
                length = info.Length,
                lastWriteTimeUtc = info.LastWriteTimeUtc,
                fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion,
                sha256 = Convert.ToHexString(hash)
            };
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new
            {
                file = settings.GameExecutable,
                status = "unavailable",
                error = exception.GetType().Name
            };
        }
    }

    private static object BuildSafeSettingsSummary(LauncherSettings settings)
    {
        return new
        {
            schemaVersion = 1,
            language = settings.Language,
            installationRoot = SanitizePath(settings.InstallRoot),
            gameExecutable = settings.GameExecutable,
            authenticationConfigured = !string.IsNullOrWhiteSpace(settings.AuthenticationEndpoint),
            authenticationTransport = settings.AuthenticationTransport,
            loginServer = settings.LoginServerAddress,
            portal = Uri.TryCreate(settings.PortalBaseUri, UriKind.Absolute, out var portal)
                ? SafeEndpoint(portal)
                : "invalid",
            discordRichPresenceEnabled = settings.DiscordRichPresenceEnabled,
            discordUsesOfficialApplication = string.Equals(
                settings.DiscordApplicationId,
                LauncherSettings.OfficialDiscordApplicationId,
                StringComparison.Ordinal),
            closeAfterLaunch = settings.CloseAfterLaunch
        };
    }

    private static string BuildTextReport(LauncherDiagnosticReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("NosGM Launcher diagnostics");
        builder.AppendLine($"Generated (UTC): {report.GeneratedAtUtc:O}");
        builder.AppendLine($"Overall: {report.OverallStatus}");
        builder.AppendLine($"OS: {report.Environment.OperatingSystem}");
        builder.AppendLine($"Runtime: {report.Environment.Runtime}");
        builder.AppendLine($"Launcher: {report.Environment.LauncherVersion}");
        builder.AppendLine($"Language: {report.Environment.Language}");
        builder.AppendLine($"Install root: {report.Environment.InstallationRoot}");
        builder.AppendLine();

        foreach (var check in report.Checks)
        {
            builder.AppendLine($"[{check.Status}] {check.Title}: {check.Summary}");
            if (!string.IsNullOrWhiteSpace(check.Details))
            {
                builder.AppendLine($"  Details: {check.Details}");
            }
            if (!string.IsNullOrWhiteSpace(check.SuggestedAction))
            {
                builder.AppendLine($"  Action: {check.SuggestedAction}");
            }
        }

        return builder.ToString();
    }

    private static async Task<byte[]> ReadBoundedAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaximumPortalResponseBytes)
        {
            throw new InvalidDataException("Portal response exceeds the diagnostic limit.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException("Portal response exceeds the diagnostic limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return destination.ToArray();
    }

    private static string SanitizePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(profile)
                && fullPath.StartsWith(profile, StringComparison.OrdinalIgnoreCase))
            {
                return "C:\\Users\\<user>" + fullPath[profile.Length..];
            }

            return fullPath;
        }
        catch
        {
            return "<invalid-path>";
        }
    }

    private static string SafeEndpoint(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri.GetLeftPart(UriPartial.Path);
    }

    private static string SafeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private static LauncherDiagnosticCheck Passed(
        string id,
        string title,
        string summary,
        string details = "")
        => new(id, title, LauncherDiagnosticStatus.Passed, summary, Details: details);

    private static LauncherDiagnosticCheck Information(
        string id,
        string title,
        string summary,
        string details = "")
        => new(id, title, LauncherDiagnosticStatus.Information, summary, Details: details);

    private static LauncherDiagnosticCheck Warning(
        string id,
        string title,
        string summary,
        string action,
        string details = "")
        => new(id, title, LauncherDiagnosticStatus.Warning, summary, action, details);

    private static LauncherDiagnosticCheck Failed(
        string id,
        string title,
        string summary,
        string action,
        string details = "")
        => new(id, title, LauncherDiagnosticStatus.Failed, summary, action, details);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }
}
