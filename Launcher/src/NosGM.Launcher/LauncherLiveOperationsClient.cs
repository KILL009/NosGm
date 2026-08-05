// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Launcher;

internal sealed record LauncherRateMultiplier(
    string Id,
    string Name,
    int Multiplier);

internal sealed record LauncherMaintenanceStatus(
    bool IsActive,
    string Title,
    string Message,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

internal sealed record LauncherCalendarEvent(
    string Id,
    string Type,
    string Title,
    string Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Channel,
    int MinimumLevel,
    int MaximumLevel,
    string Details);

internal sealed record LauncherOperationsSnapshot(
    DateTimeOffset ObservedAt,
    IReadOnlyList<LauncherRateMultiplier> Rates,
    LauncherMaintenanceStatus Maintenance,
    IReadOnlyList<LauncherCalendarEvent> Events,
    bool IsStale);

internal sealed record LauncherOperationsDashboard(
    LauncherOperationsSnapshot Operations,
    LauncherServerStatus Status);

internal sealed class LauncherLiveOperationsClient : IDisposable
{
    private const int MaximumResponseBytes = 256 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LauncherLiveOperationsClient(string portalBaseUri)
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

    public async Task<LauncherOperationsDashboard> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var operationsTask = GetJsonAsync<LauncherOperationsSnapshot>(
            "api/v1/public/operations",
            cancellationToken);
        var statusTask = GetJsonAsync<LauncherServerStatus>(
            "api/v1/public/status",
            cancellationToken);

        await Task.WhenAll(operationsTask, statusTask).ConfigureAwait(false);
        var dashboard = new LauncherOperationsDashboard(
            await operationsTask.ConfigureAwait(false),
            await statusTask.ConfigureAwait(false));
        Validate(dashboard);
        return dashboard;
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
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("NosGM operations endpoint returned a non-JSON response.");
        }

        if (response.Content.Headers.ContentLength is long length
            && (length <= 0 || length > MaximumResponseBytes))
        {
            throw new InvalidDataException("NosGM operations response size is invalid.");
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
                throw new InvalidDataException("NosGM operations response exceeded the size limit.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                .ConfigureAwait(false);
        }

        return JsonSerializer.Deserialize<T>(memory.ToArray(), JsonOptions)
               ?? throw new InvalidDataException("NosGM operations endpoint returned an empty document.");
    }

    private static void Validate(LauncherOperationsDashboard dashboard)
    {
        var operations = dashboard.Operations
                         ?? throw new InvalidDataException("Launcher operations are missing.");
        if (operations.ObservedAt > DateTimeOffset.UtcNow.AddMinutes(5)
            || operations.ObservedAt < DateTimeOffset.UtcNow.AddDays(-2)
            || operations.Rates is null
            || operations.Rates.Count is < 1 or > 20
            || operations.Rates.Select(rate => rate.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != operations.Rates.Count
            || operations.Events is null
            || operations.Events.Count > 100)
        {
            throw new InvalidDataException("Launcher operations payload is invalid.");
        }

        foreach (var rate in operations.Rates)
        {
            if (!IsSafeToken(rate.Id, 64)
                || string.IsNullOrWhiteSpace(rate.Name)
                || rate.Name.Length > 60
                || rate.Multiplier is < 0 or > 10000)
            {
                throw new InvalidDataException("Launcher operations contains an invalid rate.");
            }
        }

        var maintenance = operations.Maintenance
                          ?? throw new InvalidDataException("Launcher maintenance state is missing.");
        if (maintenance.Title.Length > 100
            || maintenance.Message.Length > 400
            || maintenance.StartsAt.HasValue != maintenance.EndsAt.HasValue
            || maintenance.StartsAt.HasValue
            && maintenance.EndsAt <= maintenance.StartsAt)
        {
            throw new InvalidDataException("Launcher maintenance state is invalid.");
        }

        foreach (var item in operations.Events)
        {
            if (!IsSafeToken(item.Id, 80)
                || !IsSafeToken(item.Type, 50)
                || !IsSafeToken(item.Category, 32)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Length > 120
                || item.Details.Length > 400
                || item.EndsAt <= item.StartsAt
                || item.Channel is < 0 or > 255
                || item.MinimumLevel is < 0 or > 255
                || item.MaximumLevel < item.MinimumLevel
                || item.MaximumLevel > 255)
            {
                throw new InvalidDataException("Launcher event calendar contains an invalid event.");
            }
        }

        if (dashboard.Status is null
            || dashboard.Status.Services is null
            || dashboard.Status.Services.Count is < 1 or > 64)
        {
            throw new InvalidDataException("Launcher channel population is invalid.");
        }
    }

    private static bool IsSafeToken(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= maximumLength
           && value.All(character =>
               char.IsLetterOrDigit(character)
               || character is '-' or '_' or '.');

    public void Dispose() => _client.Dispose();
}
