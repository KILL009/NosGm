// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Launcher;

internal enum LauncherServiceHealth
{
    Offline = 0,
    Degraded = 1,
    Online = 2
}

internal sealed record LauncherNewsItem(
    string Id,
    string Slug,
    string Title,
    string Summary,
    DateTimeOffset PublishedAt,
    string Language);

internal sealed record LauncherServiceStatus(
    string Id,
    string Name,
    LauncherServiceHealth Health,
    int OnlinePlayers);

internal sealed record LauncherServerStatus(
    string ServerName,
    LauncherServiceHealth OverallHealth,
    int OnlinePlayers,
    IReadOnlyList<LauncherServiceStatus> Services,
    DateTimeOffset ObservedAt,
    bool IsStale);

internal sealed record LauncherPortalMetadata(
    string ServerName,
    string ClientVersion,
    bool LauncherDownloadAvailable,
    IReadOnlyList<string> SupportedLanguages,
    string ApiVersion,
    string DataSource);

internal sealed record LauncherLiveContentSnapshot(
    DateTimeOffset FetchedAt,
    LauncherPortalMetadata Metadata,
    IReadOnlyList<LauncherNewsItem> News,
    LauncherServerStatus Status);

internal sealed class LauncherLiveContentClient : IDisposable
{
    private const int MaximumResponseBytes = 256 * 1024;
    private const int MaximumNewsItems = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LauncherLiveContentClient(string portalBaseUri)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            CheckCertificateRevocationList = true,
            UseCookies = false
        };
        _client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(portalBaseUri, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(6)
        };
        _client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("NosGM-Launcher", "1.0"));
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LauncherLiveContentSnapshot> GetSnapshotAsync(
        string launcherLanguage,
        CancellationToken cancellationToken)
    {
        var metadataTask = GetJsonAsync<LauncherPortalMetadata>(
            "api/v1/public/metadata",
            cancellationToken);
        var newsTask = GetJsonAsync<LauncherNewsItem[]>(
            $"api/v1/public/news?lang={Uri.EscapeDataString(ToPortalLanguage(launcherLanguage))}&limit={MaximumNewsItems}",
            cancellationToken);
        var statusTask = GetJsonAsync<LauncherServerStatus>(
            "api/v1/public/status",
            cancellationToken);

        await Task.WhenAll(metadataTask, newsTask, statusTask).ConfigureAwait(false);
        var snapshot = new LauncherLiveContentSnapshot(
            DateTimeOffset.UtcNow,
            await metadataTask.ConfigureAwait(false),
            await newsTask.ConfigureAwait(false),
            await statusTask.ConfigureAwait(false));
        LauncherLiveContentValidator.Validate(snapshot);
        return snapshot;
    }

    private async Task<T> GetJsonAsync<T>(
        string relativeUri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if ((int)response.StatusCode is < 200 or > 299)
        {
            throw new HttpRequestException(
                $"NosGM portal returned HTTP {(int)response.StatusCode} for {relativeUri}.",
                null,
                response.StatusCode);
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("NosGM portal returned a non-JSON response.");
        }

        if (response.Content.Headers.ContentLength is long length &&
            (length <= 0 || length > MaximumResponseBytes))
        {
            throw new InvalidDataException("NosGM portal response size is invalid.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (memory.Length + read > MaximumResponseBytes)
            {
                throw new InvalidDataException("NosGM portal response exceeded the size limit.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                .ConfigureAwait(false);
        }

        return JsonSerializer.Deserialize<T>(memory.ToArray(), JsonOptions)
               ?? throw new InvalidDataException("NosGM portal returned an empty JSON document.");
    }

    private static string ToPortalLanguage(string language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "cz" => "cs",
            "jp" => "ja",
            "cn" => "zh-CN",
            "es" or "en" or "de" or "fr" or "it" or "pl" or "ru" =>
                language.Trim().ToLowerInvariant(),
            _ => "en"
        };
    }

    public void Dispose() => _client.Dispose();
}

internal static class LauncherLiveContentCache
{
    private const int MaximumCacheBytes = 512 * 1024;
    private static readonly TimeSpan MaximumCacheAge = TimeSpan.FromDays(7);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string CachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "live-content-cache.json");

    public static async Task<LauncherLiveContentSnapshot?> LoadAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(CachePath))
        {
            return null;
        }

        var file = new FileInfo(CachePath);
        if (file.Length <= 0 || file.Length > MaximumCacheBytes)
        {
            return null;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(CachePath, cancellationToken)
                .ConfigureAwait(false);
            var snapshot = JsonSerializer.Deserialize<LauncherLiveContentSnapshot>(bytes, JsonOptions);
            if (snapshot is null || DateTimeOffset.UtcNow - snapshot.FetchedAt > MaximumCacheAge)
            {
                return null;
            }

            LauncherLiveContentValidator.Validate(snapshot);
            return snapshot;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return null;
        }
    }

    public static async Task SaveAsync(
        LauncherLiveContentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        LauncherLiveContentValidator.Validate(snapshot);
        var directory = Path.GetDirectoryName(CachePath)
                        ?? throw new InvalidOperationException("Live content cache directory is unavailable.");
        Directory.CreateDirectory(directory);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        if (bytes.Length <= 0 || bytes.Length > MaximumCacheBytes)
        {
            throw new InvalidDataException("Live content cache size is invalid.");
        }

        var temporaryPath = CachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryPath, CachePath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch
            {
                // A temporary cache file can be cleaned on a later run.
            }
        }
    }
}

internal static class LauncherLiveContentValidator
{
    public static void Validate(LauncherLiveContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.FetchedAt > DateTimeOffset.UtcNow.AddMinutes(5) ||
            snapshot.FetchedAt < DateTimeOffset.UtcNow.AddDays(-30))
        {
            throw new InvalidDataException("Live content fetch time is invalid.");
        }

        if (snapshot.Metadata is null ||
            string.IsNullOrWhiteSpace(snapshot.Metadata.ServerName) ||
            snapshot.Metadata.ServerName.Length > 40 ||
            string.IsNullOrWhiteSpace(snapshot.Metadata.ClientVersion) ||
            snapshot.Metadata.ClientVersion.Length > 32 ||
            !string.Equals(snapshot.Metadata.ApiVersion, "v1", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Metadata.SupportedLanguages is null ||
            snapshot.Metadata.SupportedLanguages.Count is < 1 or > 20)
        {
            throw new InvalidDataException("Live portal metadata is invalid.");
        }

        if (snapshot.News is null || snapshot.News.Count > 3)
        {
            throw new InvalidDataException("Live news collection is invalid.");
        }

        foreach (var item in snapshot.News)
        {
            if (item is null ||
                !IsSafeToken(item.Id, 80) ||
                !IsSafeToken(item.Slug, 100) ||
                string.IsNullOrWhiteSpace(item.Title) ||
                item.Title.Length > 160 ||
                string.IsNullOrWhiteSpace(item.Summary) ||
                item.Summary.Length > 600 ||
                item.PublishedAt <= DateTimeOffset.UnixEpoch ||
                item.PublishedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                throw new InvalidDataException("Live news item is invalid.");
            }
        }

        var status = snapshot.Status;
        if (status is null ||
            string.IsNullOrWhiteSpace(status.ServerName) ||
            status.ServerName.Length > 40 ||
            status.OnlinePlayers < 0 ||
            status.Services is null ||
            status.Services.Count is < 1 or > 64 ||
            status.ObservedAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new InvalidDataException("Live server status is invalid.");
        }

        if (status.Services.Select(service => service.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != status.Services.Count)
        {
            throw new InvalidDataException("Live server status contains duplicate services.");
        }

        foreach (var service in status.Services)
        {
            if (service is null ||
                !IsSafeToken(service.Id, 64) ||
                string.IsNullOrWhiteSpace(service.Name) ||
                service.Name.Length > 80 ||
                service.OnlinePlayers < 0)
            {
                throw new InvalidDataException("Live service status is invalid.");
            }
        }
    }

    private static bool IsSafeToken(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length <= maximumLength &&
           value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
}
