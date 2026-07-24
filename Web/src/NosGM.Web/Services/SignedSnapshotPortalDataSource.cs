// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NosGM.Web.Contracts;
using NosGM.Web.Localization;

namespace NosGM.Web.Services;

public sealed class SignedSnapshotPortalDataSource : IPortalDataSource, IPublicDataHealth
{
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PublicDataOptions _options;
    private readonly PortalOptions _portalOptions;
    private readonly string _snapshotPath;
    private readonly ILogger<SignedSnapshotPortalDataSource> _logger;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly object _stateLock = new();

    private SnapshotPayload? _snapshot;
    private DateTime _loadedWriteTimeUtc;
    private string? _lastError;

    public SignedSnapshotPortalDataSource(
        IOptions<PublicDataOptions> options,
        IOptions<PortalOptions> portalOptions,
        IHostEnvironment environment,
        ILogger<SignedSnapshotPortalDataSource> logger)
    {
        _options = options.Value;
        _portalOptions = portalOptions.Value;
        _snapshotPath = _options.ResolveSnapshotPath(environment.ContentRootPath);
        _logger = logger;
    }

    public bool IsReady
    {
        get
        {
            lock (_stateLock)
            {
                return _snapshot is not null && !IsStale(_snapshot.ObservedAt, DateTimeOffset.UtcNow);
            }
        }
    }

    public DateTimeOffset? ObservedAt
    {
        get
        {
            lock (_stateLock)
            {
                return _snapshot?.ObservedAt;
            }
        }
    }

    public string? LastError
    {
        get
        {
            lock (_stateLock)
            {
                return _lastError;
            }
        }
    }

    public async ValueTask<IReadOnlyList<PublicNewsItem>> GetNewsAsync(
        string language,
        int limit,
        CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var normalizedLanguage = PortalCulture.Normalize(language);
        var items = snapshot.News
            .Where(item => string.Equals(item.Language, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.PublishedAt)
            .Take(Math.Clamp(limit, 1, 20))
            .ToArray();

        if (items.Length == 0 && !string.Equals(normalizedLanguage, "en", StringComparison.OrdinalIgnoreCase))
        {
            items = snapshot.News
                .Where(item => string.Equals(item.Language, "en", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.PublishedAt)
                .Take(Math.Clamp(limit, 1, 20))
                .ToArray();
        }

        return items;
    }

    public async ValueTask<PublicServerStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var stale = IsStale(snapshot.ObservedAt, now);
        var services = snapshot.Services.ToArray();
        var onlinePlayers = services
            .Where(service => service.Id.StartsWith("channel-", StringComparison.OrdinalIgnoreCase))
            .Sum(service => Math.Max(0, service.OnlinePlayers));

        var healthyServices = services.Count(service => service.Health == ServiceHealth.Online);
        var overall = healthyServices switch
        {
            0 => ServiceHealth.Offline,
            _ when healthyServices == services.Length => ServiceHealth.Online,
            _ => ServiceHealth.Degraded
        };

        if (stale)
        {
            var age = now - snapshot.ObservedAt;
            overall = age > TimeSpan.FromSeconds(_options.MaximumAgeSeconds * 3L)
                ? ServiceHealth.Offline
                : ServiceHealth.Degraded;
        }

        return new PublicServerStatus(
            snapshot.ServerName,
            overall,
            onlinePlayers,
            services,
            snapshot.ObservedAt,
            stale);
    }

    public async ValueTask<IReadOnlyList<PublicRankingEntry>> GetRankingsAsync(
        RankingKind kind,
        int limit,
        CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var key = kind.ToString().ToLowerInvariant();
        if (!snapshot.Rankings.TryGetValue(key, out var entries))
        {
            return [];
        }

        return entries
            .OrderBy(entry => entry.Position)
            .Take(Math.Clamp(limit, 1, 50))
            .ToArray();
    }

    public static string ComputeSignatureBase64(
        int schemaVersion,
        string keyId,
        string payloadJson,
        ReadOnlySpan<byte> key)
    {
        var signedText = $"{schemaVersion}\n{keyId}\n{payloadJson}";
        var signature = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedText));
        return Convert.ToBase64String(signature);
    }

    private async ValueTask<SnapshotPayload> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_options.TryGetHmacKey(out var key))
            {
                return UseLastSnapshotOrUnavailable(
                    "Public snapshot HMAC key is missing or shorter than 32 bytes.");
            }

            var file = new FileInfo(_snapshotPath);
            if (!file.Exists)
            {
                return UseLastSnapshotOrUnavailable("Public snapshot file does not exist.");
            }

            if (file.Length <= 0 || file.Length > _options.MaximumSnapshotBytes)
            {
                return UseLastSnapshotOrUnavailable("Public snapshot file has an invalid size.");
            }

            if (_snapshot is not null && file.LastWriteTimeUtc == _loadedWriteTimeUtc)
            {
                return _snapshot;
            }

            var json = await File.ReadAllTextAsync(_snapshotPath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            var envelope = JsonSerializer.Deserialize<SnapshotEnvelope>(json, SnapshotJsonOptions)
                ?? throw new InvalidDataException("Snapshot envelope is empty.");

            if (envelope.SchemaVersion != SupportedSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported snapshot schema {envelope.SchemaVersion}.");
            }

            if (!string.Equals(envelope.KeyId, _options.KeyId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Snapshot key id does not match configuration.");
            }

            var payloadJson = envelope.Payload.GetRawText();
            var expected = ComputeSignatureBase64(
                envelope.SchemaVersion,
                envelope.KeyId,
                payloadJson,
                key);

            if (!TryDecodeSignature(expected, out var expectedBytes)
                || !TryDecodeSignature(envelope.Signature, out var actualBytes)
                || expectedBytes.Length != actualBytes.Length
                || !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
            {
                throw new InvalidDataException("Snapshot signature validation failed.");
            }

            var snapshot = envelope.Payload.Deserialize<SnapshotPayload>(SnapshotJsonOptions)
                ?? throw new InvalidDataException("Snapshot payload is empty.");
            ValidateSnapshot(snapshot);

            lock (_stateLock)
            {
                _snapshot = snapshot;
                _loadedWriteTimeUtc = file.LastWriteTimeUtc;
                _lastError = null;
            }

            return snapshot;
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or JsonException
                                          or InvalidDataException)
        {
            _logger.LogWarning(exception, "NosGM public snapshot could not be loaded from {SnapshotPath}.", _snapshotPath);
            return UseLastSnapshotOrUnavailable("Public snapshot validation or loading failed.");
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private SnapshotPayload UseLastSnapshotOrUnavailable(string error)
    {
        lock (_stateLock)
        {
            _lastError = error;
            return _snapshot ?? CreateUnavailableSnapshot();
        }
    }

    private SnapshotPayload CreateUnavailableSnapshot()
        => new(
            _portalOptions.ServerName,
            DateTimeOffset.UnixEpoch,
            [],
            [
                new PublicServiceStatus("login", "Login", ServiceHealth.Offline, 0),
                new PublicServiceStatus("world", "World", ServiceHealth.Offline, 0),
                new PublicServiceStatus("channel-1", "Channel 1", ServiceHealth.Offline, 0)
            ],
            new Dictionary<string, IReadOnlyList<PublicRankingEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                ["combat"] = [],
                ["reputation"] = [],
                ["hero"] = []
            });

    private bool IsStale(DateTimeOffset observedAt, DateTimeOffset now)
        => observedAt == DateTimeOffset.UnixEpoch
           || now - observedAt > TimeSpan.FromSeconds(_options.MaximumAgeSeconds);

    private static bool TryDecodeSignature(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static void ValidateSnapshot(SnapshotPayload snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ServerName) || snapshot.ServerName.Length > 40)
        {
            throw new InvalidDataException("Snapshot server name is invalid.");
        }

        if (snapshot.ObservedAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new InvalidDataException("Snapshot observation time is in the future.");
        }

        if (snapshot.News.Count > 200 || snapshot.Services.Count is < 1 or > 64)
        {
            throw new InvalidDataException("Snapshot collection limits were exceeded.");
        }

        if (snapshot.Services.Select(service => service.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != snapshot.Services.Count)
        {
            throw new InvalidDataException("Snapshot contains duplicate service ids.");
        }

        foreach (var service in snapshot.Services)
        {
            if (!IsSafeToken(service.Id, 64)
                || string.IsNullOrWhiteSpace(service.Name)
                || service.Name.Length > 80
                || service.OnlinePlayers < 0)
            {
                throw new InvalidDataException("Snapshot contains an invalid service.");
            }
        }

        foreach (var news in snapshot.News)
        {
            if (!IsSafeToken(news.Id, 80)
                || !IsSafeToken(news.Slug, 100)
                || !PortalCulture.SupportedLanguages.Any(item =>
                    string.Equals(item.Code, news.Language, StringComparison.OrdinalIgnoreCase))
                || string.IsNullOrWhiteSpace(news.Title)
                || news.Title.Length > 160
                || string.IsNullOrWhiteSpace(news.Summary)
                || news.Summary.Length > 600)
            {
                throw new InvalidDataException("Snapshot contains an invalid news item.");
            }
        }

        var allowedRankings = new HashSet<string>(["combat", "reputation", "hero"], StringComparer.OrdinalIgnoreCase);
        foreach (var ranking in snapshot.Rankings)
        {
            if (!allowedRankings.Contains(ranking.Key) || ranking.Value.Count > 100)
            {
                throw new InvalidDataException("Snapshot contains an unsupported ranking.");
            }

            if (ranking.Value.Select(entry => entry.Position).Distinct().Count() != ranking.Value.Count)
            {
                throw new InvalidDataException("Snapshot contains duplicate ranking positions.");
            }

            foreach (var entry in ranking.Value)
            {
                if (entry.Position <= 0
                    || string.IsNullOrWhiteSpace(entry.CharacterName)
                    || entry.CharacterName.Length > 32
                    || entry.Level < 0
                    || entry.HeroLevel < 0
                    || entry.Reputation < 0
                    || entry.Score < 0
                    || entry.Metric.Length > 32)
                {
                    throw new InvalidDataException("Snapshot contains an invalid ranking entry.");
                }
            }
        }
    }

    private static bool IsSafeToken(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= maximumLength
           && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');

    private sealed record SnapshotEnvelope(
        int SchemaVersion,
        string KeyId,
        JsonElement Payload,
        string Signature);

    private sealed record SnapshotPayload(
        string ServerName,
        DateTimeOffset ObservedAt,
        IReadOnlyList<PublicNewsItem> News,
        IReadOnlyList<PublicServiceStatus> Services,
        IReadOnlyDictionary<string, IReadOnlyList<PublicRankingEntry>> Rankings);
}
