// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Launcher;

internal sealed record LauncherAuthorizationTicket(
    string AuthorizationCode,
    string AccountName,
    int ExpiresInSeconds);

internal sealed class LauncherAuthenticationClient
{
    private const int MaximumResponseBytes = 16 * 1024;

    private sealed record TicketRequest(
        [property: JsonPropertyName("accountName")] string AccountName,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("installationId")] string InstallationId,
        [property: JsonPropertyName("countryId")] byte CountryId);

    private sealed record TicketResponse(
        [property: JsonPropertyName("authorizationCode")] string AuthorizationCode,
        [property: JsonPropertyName("accountName")] string AccountName,
        [property: JsonPropertyName("expiresInSeconds")] int ExpiresInSeconds);

    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);

    public async Task<LauncherAuthorizationTicket> RequestTicketAsync(
        LauncherSettings settings,
        string accountName,
        string password,
        byte countryId,
        string installationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AuthenticationEndpoint))
        {
            throw new InvalidOperationException(
                "Modern login is not configured. Set AuthenticationEndpoint in the launcher settings or NOSGM_AUTH_ENDPOINT.");
        }

        if (!Uri.TryCreate(settings.AuthenticationEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidDataException("The launcher authentication endpoint is invalid.");
        }

        if (!Guid.TryParse(installationId, out var parsedInstallationId) || parsedInstallationId == Guid.Empty)
        {
            throw new InvalidDataException("The launcher InstallationId is invalid.");
        }

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            CheckCertificateRevocationList = true
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        var requestModel = new TicketRequest(
            accountName,
            password,
            parsedInstallationId.ToString("D"),
            countryId);
        var json = JsonSerializer.Serialize(requestModel);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var responseBody = await ReadBoundedResponseAsync(response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryReadError(responseBody);
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new InvalidOperationException("La cuenta o la contraseña no son válidas."),
                HttpStatusCode.TooManyRequests => new InvalidOperationException("Demasiados intentos. Inténtalo nuevamente en un momento."),
                HttpStatusCode.ServiceUnavailable when error == "maintenance" =>
                    new InvalidOperationException("El servidor está en mantenimiento."),
                HttpStatusCode.ServiceUnavailable =>
                    new InvalidOperationException("El servicio de autenticación no está disponible."),
                _ => new InvalidOperationException("El servicio de autenticación rechazó la solicitud.")
            };
        }

        TicketResponse? ticket;
        try
        {
            ticket = JsonSerializer.Deserialize<TicketResponse>(responseBody);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The authentication server returned invalid JSON.", exception);
        }

        if (ticket is null ||
            !Guid.TryParse(ticket.AuthorizationCode, out var authorizationCode) ||
            authorizationCode == Guid.Empty ||
            string.IsNullOrWhiteSpace(ticket.AccountName) ||
            ticket.ExpiresInSeconds is < 15 or > 600)
        {
            throw new InvalidDataException("The authentication server returned an invalid ticket.");
        }

        return new LauncherAuthorizationTicket(
            authorizationCode.ToString("D"),
            ticket.AccountName,
            ticket.ExpiresInSeconds);
    }

    private static async Task<string> ReadBoundedResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is > MaximumResponseBytes)
        {
            throw new InvalidDataException("The authentication response is too large.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[2048];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (memory.Length + read > MaximumResponseBytes)
            {
                throw new InvalidDataException("The authentication response exceeded the maximum size.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static string? TryReadError(string responseBody)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(responseBody)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
