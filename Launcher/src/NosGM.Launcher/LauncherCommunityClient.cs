// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Launcher;

internal sealed record LauncherRankingEntry(
    int Position,
    string CharacterName,
    int Level,
    int HeroLevel,
    long Reputation,
    long Score,
    string Metric);

internal sealed record LauncherCommunitySnapshot(
    DateTimeOffset FetchedAt,
    LauncherServerStatus Status,
    LauncherMaintenanceStatus Maintenance,
    IReadOnlyList<LauncherCalendarEvent> Events,
    IReadOnlyList<LauncherNewsItem> News,
    IReadOnlyList<LauncherRankingEntry> CombatRanking,
    IReadOnlyList<LauncherRankingEntry> ReputationRanking,
    IReadOnlyList<LauncherRankingEntry> HeroRanking);

internal sealed class LauncherCommunityClient : IDisposable
{
    private const int MaximumResponseBytes = 256 * 1024;
    private const int MaximumNewsItems = 12;
    private const int MaximumRankingEntries = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LauncherCommunityClient(string portalBaseUri)
    {
        if (!TryValidatePortalBaseUri(portalBaseUri, out var baseUri))
        {
            throw new InvalidDataException("The NosGM community portal URI is invalid.");
        }

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            CheckCertificateRevocationList = true,
            UseCookies = false
        };
        _client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(8)
        };
        _client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("NosGM-Launcher", "1.0"));
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LauncherCommunitySnapshot> GetSnapshotAsync(
        string launcherLanguage,
        CancellationToken cancellationToken)
    {
        var portalLanguage = ToPortalLanguage(launcherLanguage);
        var statusTask = GetJsonAsync<LauncherServerStatus>(
            "api/v1/public/status",
            cancellationToken);
        var operationsTask = GetJsonAsync<LauncherOperationsSnapshot>(
            "api/v1/public/operations",
            cancellationToken);
        var newsTask = GetJsonAsync<LauncherNewsItem[]>(
            $"api/v1/public/news?lang={Uri.EscapeDataString(portalLanguage)}&limit={MaximumNewsItems}",
            cancellationToken);
        var combatTask = GetJsonAsync<LauncherRankingEntry[]>(
            $"api/v1/public/rankings/combat?limit={MaximumRankingEntries}",
            cancellationToken);
        var reputationTask = GetJsonAsync<LauncherRankingEntry[]>(
            $"api/v1/public/rankings/reputation?limit={MaximumRankingEntries}",
            cancellationToken);
        var heroTask = GetJsonAsync<LauncherRankingEntry[]>(
            $"api/v1/public/rankings/hero?limit={MaximumRankingEntries}",
            cancellationToken);

        await Task.WhenAll(
                statusTask,
                operationsTask,
                newsTask,
                combatTask,
                reputationTask,
                heroTask)
            .ConfigureAwait(false);

        var operations = await operationsTask.ConfigureAwait(false);
        var snapshot = new LauncherCommunitySnapshot(
            DateTimeOffset.UtcNow,
            await statusTask.ConfigureAwait(false),
            operations.Maintenance,
            operations.Events,
            await newsTask.ConfigureAwait(false),
            await combatTask.ConfigureAwait(false),
            await reputationTask.ConfigureAwait(false),
            await heroTask.ConfigureAwait(false));
        LauncherCommunityValidator.Validate(snapshot);
        return snapshot;
    }

    private async Task<T> GetJsonAsync<T>(
        string relativeUri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        using var response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);
        if ((int)response.StatusCode is < 200 or > 299)
        {
            throw new HttpRequestException(
                $"NosGM portal returned HTTP {(int)response.StatusCode} for community data.",
                null,
                response.StatusCode);
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("NosGM portal returned a non-JSON community response.");
        }

        if (response.Content.Headers.ContentLength is long length
            && (length <= 0 || length > MaximumResponseBytes))
        {
            throw new InvalidDataException("NosGM community response size is invalid.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (memory.Length + read > MaximumResponseBytes)
            {
                throw new InvalidDataException("NosGM community response exceeded the size limit.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                .ConfigureAwait(false);
        }

        return JsonSerializer.Deserialize<T>(memory.ToArray(), JsonOptions)
               ?? throw new InvalidDataException("NosGM portal returned an empty community document.");
    }

    internal static string ToPortalLanguage(string? language)
        => language?.Trim().ToLowerInvariant() switch
        {
            "cz" => "cs",
            "jp" => "ja",
            "cn" => "zh-CN",
            "es" or "en" or "de" or "fr" or "it" or "pl" or "ru" =>
                language.Trim().ToLowerInvariant(),
            _ => "en"
        };

    private static bool TryValidatePortalBaseUri(string value, out Uri baseUri)
    {
        baseUri = null!;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var candidate)
            || candidate is null
            || !string.IsNullOrEmpty(candidate.UserInfo)
            || !string.IsNullOrEmpty(candidate.Query)
            || !string.IsNullOrEmpty(candidate.Fragment)
            || !candidate.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var transportAllowed =
            string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || candidate.IsLoopback
            && string.Equals(candidate.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if (!transportAllowed)
        {
            return false;
        }

        baseUri = candidate;
        return true;
    }

    public void Dispose() => _client.Dispose();
}

internal static class LauncherCommunityCache
{
    private const int MaximumCacheBytes = 1024 * 1024;
    private static readonly TimeSpan MaximumCacheAge = TimeSpan.FromDays(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string CachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "community-cache.json");

    public static async Task<LauncherCommunitySnapshot?> LoadAsync(
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
            var snapshot = JsonSerializer.Deserialize<LauncherCommunitySnapshot>(bytes, JsonOptions);
            if (snapshot is null || DateTimeOffset.UtcNow - snapshot.FetchedAt > MaximumCacheAge)
            {
                return null;
            }

            LauncherCommunityValidator.Validate(snapshot);
            return snapshot;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return null;
        }
    }

    public static async Task SaveAsync(
        LauncherCommunitySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        LauncherCommunityValidator.Validate(snapshot);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        if (bytes.Length <= 0 || bytes.Length > MaximumCacheBytes)
        {
            throw new InvalidDataException("NosGM community cache size is invalid.");
        }

        var directory = Path.GetDirectoryName(CachePath)
                        ?? throw new InvalidOperationException("Community cache directory is unavailable.");
        Directory.CreateDirectory(directory);
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
                // A temporary public-data cache can be cleaned on a later run.
            }
        }
    }
}

internal static class LauncherCommunityValidator
{
    public static void Validate(LauncherCommunitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.FetchedAt > DateTimeOffset.UtcNow.AddMinutes(5)
            || snapshot.FetchedAt < DateTimeOffset.UtcNow.AddDays(-7))
        {
            throw new InvalidDataException("Community fetch time is invalid.");
        }

        ValidateStatus(snapshot.Status);
        ValidateMaintenance(snapshot.Maintenance);
        ValidateEvents(snapshot.Events);
        ValidateNews(snapshot.News);
        ValidateRanking(snapshot.CombatRanking);
        ValidateRanking(snapshot.ReputationRanking);
        ValidateRanking(snapshot.HeroRanking);
    }

    private static void ValidateStatus(LauncherServerStatus status)
    {
        if (status is null
            || string.IsNullOrWhiteSpace(status.ServerName)
            || status.ServerName.Length > 40
            || status.OnlinePlayers < 0
            || status.Services is null
            || status.Services.Count is < 1 or > 64
            || status.Services.Any(service => service is null)
            || status.ObservedAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new InvalidDataException("Community server status is invalid.");
        }

        if (status.Services.Select(service => service.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != status.Services.Count)
        {
            throw new InvalidDataException("Community server status contains duplicate services.");
        }
    }

    private static void ValidateMaintenance(LauncherMaintenanceStatus maintenance)
    {
        if (maintenance is null
            || maintenance.Title.Length > 100
            || maintenance.Message.Length > 400
            || maintenance.StartsAt.HasValue != maintenance.EndsAt.HasValue
            || maintenance.StartsAt.HasValue && maintenance.EndsAt <= maintenance.StartsAt)
        {
            throw new InvalidDataException("Community maintenance status is invalid.");
        }
    }

    private static void ValidateEvents(IReadOnlyList<LauncherCalendarEvent> events)
    {
        if (events is null || events.Count > 100 || events.Any(item => item is null))
        {
            throw new InvalidDataException("Community event collection is invalid.");
        }

        foreach (var item in events)
        {
            if (!IsSafeToken(item.Id, 80)
                || !IsSafeToken(item.Type, 50)
                || !IsSafeToken(item.Category, 32)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Length > 120
                || item.Details.Length > 400
                || item.EndsAt <= item.StartsAt
                || item.StartsAt < DateTimeOffset.UtcNow.AddYears(-5)
                || item.EndsAt > DateTimeOffset.UtcNow.AddYears(5)
                || item.Channel is < 0 or > 255
                || item.MinimumLevel is < 0 or > 255
                || item.MaximumLevel < item.MinimumLevel
                || item.MaximumLevel > 255)
            {
                throw new InvalidDataException("Community calendar contains an invalid event.");
            }
        }
    }

    private static void ValidateNews(IReadOnlyList<LauncherNewsItem> news)
    {
        if (news is null || news.Count > 12 || news.Any(item => item is null))
        {
            throw new InvalidDataException("Community news collection is invalid.");
        }

        foreach (var item in news)
        {
            if (!IsSafeToken(item.Id, 80)
                || !IsSafeToken(item.Slug, 100)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Length > 160
                || string.IsNullOrWhiteSpace(item.Summary)
                || item.Summary.Length > 600
                || item.PublishedAt <= DateTimeOffset.UnixEpoch
                || item.PublishedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                throw new InvalidDataException("Community news contains an invalid item.");
            }
        }
    }

    private static void ValidateRanking(IReadOnlyList<LauncherRankingEntry> entries)
    {
        if (entries is null
            || entries.Count > 20
            || entries.Any(entry => entry is null)
            || entries.Select(entry => entry.Position).Distinct().Count() != entries.Count)
        {
            throw new InvalidDataException("Community ranking collection is invalid.");
        }

        foreach (var entry in entries)
        {
            if (entry.Position <= 0
                || string.IsNullOrWhiteSpace(entry.CharacterName)
                || entry.CharacterName.Length > 32
                || entry.CharacterName.Any(char.IsControl)
                || entry.Level is < 0 or > 255
                || entry.HeroLevel is < 0 or > 255
                || entry.Reputation < 0
                || entry.Score < 0
                || !IsSafeToken(entry.Metric, 32))
            {
                throw new InvalidDataException("Community ranking contains an invalid entry.");
            }
        }
    }

    private static bool IsSafeToken(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= maximumLength
           && value.All(character =>
               char.IsLetterOrDigit(character)
               || character is '-' or '_' or '.');
}
