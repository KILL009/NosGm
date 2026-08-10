using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.LoadTest;

internal sealed record ModernLoginTicket(
    string AuthorizationCode,
    string AccountName,
    int ExpiresInSeconds);

internal static class ModernLoginTicketClient
{
    private static readonly HttpClient Client = CreateHttpClient();

    public static async Task<ModernLoginTicket> IssueAsync(
        LoadTestOptions options,
        LoadAccount account,
        Guid installationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(account);
        if (installationId == Guid.Empty)
        {
            throw new ArgumentException("Modern Login requires a non-empty InstallationId.", nameof(installationId));
        }

        var request = new TicketRequest
        {
            AccountName = account.Username,
            Password = account.Password,
            InstallationId = installationId.ToString("D"),
            CountryId = options.Region
        };

        using var requestCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestCancellation.CancelAfter(options.ReadTimeoutMilliseconds);

        string json = JsonSerializer.Serialize(request);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, options.AuthBridgeUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Accept.ParseAdd("application/json");

        // LauncherAuthBridge rejects requests without a positive Content-Length.
        // Match the production launcher framing instead of using PostAsJsonAsync,
        // whose streaming content can arrive at HttpListener with ContentLength64=-1.
        requestMessage.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);

        using HttpResponseMessage response = await Client
            .SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                requestCancellation.Token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"AuthBridge rejected the ticket request with HTTP {(int)response.StatusCode}.");
        }

        TicketResponse? payload = await response.Content
            .ReadFromJsonAsync<TicketResponse>(cancellationToken: requestCancellation.Token)
            .ConfigureAwait(false);

        if (payload == null ||
            string.IsNullOrWhiteSpace(payload.AuthorizationCode) ||
            string.IsNullOrWhiteSpace(payload.AccountName) ||
            payload.ExpiresInSeconds <= 0)
        {
            throw new InvalidOperationException("AuthBridge returned an invalid ticket response.");
        }

        if (!string.Equals(payload.AccountName, account.Username, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AuthBridge returned a ticket for a different account.");
        }

        return new ModernLoginTicket(
            payload.AuthorizationCode,
            payload.AccountName,
            payload.ExpiresInSeconds);
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 4096,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30)
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public sealed class TicketRequest
    {
        [JsonPropertyName("accountName")]
        public string AccountName { get; init; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; init; } = string.Empty;

        [JsonPropertyName("installationId")]
        public string InstallationId { get; init; } = string.Empty;

        [JsonPropertyName("countryId")]
        public byte CountryId { get; init; }
    }

    public sealed class TicketResponse
    {
        [JsonPropertyName("authorizationCode")]
        public string AuthorizationCode { get; init; } = string.Empty;

        [JsonPropertyName("accountName")]
        public string AccountName { get; init; } = string.Empty;

        [JsonPropertyName("expiresInSeconds")]
        public int ExpiresInSeconds { get; init; }
    }
}
