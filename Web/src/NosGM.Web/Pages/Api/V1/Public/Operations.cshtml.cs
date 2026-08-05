// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NosGM.Web.Contracts;

namespace NosGM.Web.Pages.Api.V1.Public;

[IgnoreAntiforgeryToken]
[EnableRateLimiting("public-api")]
public sealed class OperationsModel : PageModel
{
    private const int SupportedSchemaVersion = 1;
    private const int MaximumOperationsBytes = 256 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PublicDataOptions _options;
    private readonly string _operationsPath;
    private readonly ILogger<OperationsModel> _logger;

    public OperationsModel(
        IOptions<PublicDataOptions> options,
        IHostEnvironment environment,
        ILogger<OperationsModel> logger)
    {
        _options = options.Value;
        var snapshotPath = _options.ResolveSnapshotPath(environment.ContentRootPath);
        var directory = Path.GetDirectoryName(snapshotPath)
                        ?? throw new InvalidOperationException("Public data directory is unavailable.");
        _operationsPath = Path.Combine(directory, "public-operations.json");
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "public,max-age=10,stale-while-revalidate=30";

        try
        {
            var snapshot = await ReadAndValidateAsync(cancellationToken).ConfigureAwait(false);
            return new JsonResult(snapshot);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _logger.LogWarning(
                exception,
                "NosGM public operations could not be loaded from {OperationsPath}.",
                _operationsPath);
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Launcher operations are temporarily unavailable."
            };
            var result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            result.ContentTypes.Add("application/problem+json");
            return result;
        }
    }

    private async Task<PublicOperationsSnapshot> ReadAndValidateAsync(
        CancellationToken cancellationToken)
    {
        if (!_options.TryGetHmacKey(out var key))
        {
            throw new InvalidDataException("Public operations HMAC key is unavailable.");
        }

        try
        {
            var file = new FileInfo(_operationsPath);
            if (!file.Exists || file.Length <= 0 || file.Length > MaximumOperationsBytes)
            {
                throw new InvalidDataException("Public operations file size is invalid.");
            }

            var json = await System.IO.File.ReadAllTextAsync(
                    _operationsPath,
                    Encoding.UTF8,
                    cancellationToken)
                .ConfigureAwait(false);
            var envelope = JsonSerializer.Deserialize<OperationsEnvelope>(json, JsonOptions)
                           ?? throw new InvalidDataException("Public operations envelope is empty.");

            if (envelope.SchemaVersion != SupportedSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported public operations schema {envelope.SchemaVersion}.");
            }

            if (!string.Equals(envelope.KeyId, _options.KeyId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Public operations key id does not match configuration.");
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
                throw new InvalidDataException("Public operations signature validation failed.");
            }

            var payload = envelope.Payload.Deserialize<OperationsPayload>(JsonOptions)
                          ?? throw new InvalidDataException("Public operations payload is empty.");
            ValidatePayload(payload);

            var stale = payload.ObservedAt == DateTimeOffset.UnixEpoch
                        || DateTimeOffset.UtcNow - payload.ObservedAt
                        > TimeSpan.FromSeconds(_options.MaximumAgeSeconds);
            return new PublicOperationsSnapshot(
                payload.ObservedAt,
                payload.Rates,
                payload.Maintenance,
                payload.Events,
                stale);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static void ValidatePayload(OperationsPayload payload)
    {
        var now = DateTimeOffset.UtcNow;
        if (payload.ObservedAt > now.AddMinutes(5)
            || payload.ObservedAt < now.AddDays(-2))
        {
            throw new InvalidDataException("Public operations observation time is invalid.");
        }

        if (payload.Rates is null
            || payload.Rates.Count is < 1 or > 20
            || payload.Rates.Select(rate => rate.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != payload.Rates.Count)
        {
            throw new InvalidDataException("Public operations rates are invalid.");
        }

        foreach (var rate in payload.Rates)
        {
            if (!IsSafeToken(rate.Id, 64)
                || string.IsNullOrWhiteSpace(rate.Name)
                || rate.Name.Length > 60
                || rate.Multiplier is < 0 or > 10000)
            {
                throw new InvalidDataException("Public operations contains an invalid rate.");
            }
        }

        var maintenance = payload.Maintenance
                          ?? throw new InvalidDataException("Public maintenance status is missing.");
        if (maintenance.Title.Length > 100
            || maintenance.Message.Length > 400
            || maintenance.StartsAt.HasValue != maintenance.EndsAt.HasValue
            || maintenance.StartsAt.HasValue
            && maintenance.EndsAt <= maintenance.StartsAt)
        {
            throw new InvalidDataException("Public maintenance status is invalid.");
        }

        if (payload.Events is null
            || payload.Events.Count > 100
            || payload.Events.Select(item => item.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != payload.Events.Count)
        {
            throw new InvalidDataException("Public event calendar is invalid.");
        }

        foreach (var item in payload.Events)
        {
            if (!IsSafeToken(item.Id, 80)
                || !IsSafeToken(item.Type, 50)
                || !IsSafeToken(item.Category, 32)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Length > 120
                || item.Details.Length > 400
                || item.StartsAt <= DateTimeOffset.UnixEpoch
                || item.EndsAt <= item.StartsAt
                || item.StartsAt > now.AddDays(31)
                || item.EndsAt < now.AddDays(-1)
                || item.Channel is < 0 or > 255
                || item.MinimumLevel is < 0 or > 255
                || item.MaximumLevel < item.MinimumLevel
                || item.MaximumLevel > 255)
            {
                throw new InvalidDataException("Public event calendar contains an invalid event.");
            }
        }
    }

    private static string ComputeSignatureBase64(
        int schemaVersion,
        string keyId,
        string payloadJson,
        ReadOnlySpan<byte> key)
    {
        var signedText = $"{schemaVersion}\n{keyId}\n{payloadJson}";
        var signature = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedText));
        return Convert.ToBase64String(signature);
    }

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

    private static bool IsSafeToken(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= maximumLength
           && value.All(character =>
               char.IsLetterOrDigit(character)
               || character is '-' or '_' or '.');

    private sealed record OperationsEnvelope(
        int SchemaVersion,
        string KeyId,
        JsonElement Payload,
        string Signature);

    private sealed record OperationsPayload(
        DateTimeOffset ObservedAt,
        IReadOnlyList<PublicRateMultiplier> Rates,
        PublicMaintenanceStatus Maintenance,
        IReadOnlyList<PublicCalendarEvent> Events);
}
