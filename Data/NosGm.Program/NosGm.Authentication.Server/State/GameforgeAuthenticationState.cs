using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.Authentication.V1;

namespace NosGm.Authentication.Server.State;

public sealed class GameforgeAuthenticationState
{
    public const int MaximumOutstandingTickets = 10000;
    public static readonly TimeSpan MaximumActiveSessionLifetime =
        TimeSpan.FromHours(24);
    public const int MaximumOutstandingPermits = 10000;

    private sealed class Ticket
    {
        public required string AccountName { get; init; }

        public required Guid InstallationId { get; init; }

        public required uint CountryId { get; init; }

        public required DateTimeOffset ExpiresAt { get; set; }

        public int ConsumptionCount { get; set; }

        public int SessionId { get; set; }
    }

    private sealed class Permit
    {
        public required DateTimeOffset ExpiresAt { get; init; }

        public required string IpAddress { get; init; }
    }

    private readonly ConcurrentDictionary<string, Ticket> _tickets =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Permit> _permits =
        new(StringComparer.Ordinal);
    private readonly object _permitIssueLock = new();
    private readonly object _ticketIssueLock = new();
    private readonly TimeProvider _timeProvider;

    public GameforgeAuthenticationState(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public int TicketCount => _tickets.Count;

    public int PermitCount => _permits.Count;

    public AuthenticationTransportResultCode TryIssueTicket(
        string accountName,
        string authorizationCode,
        Guid installationId,
        uint countryId,
        TimeSpan lifetime)
    {
        return TryIssueTicketCore(
            accountName,
            authorizationCode,
            installationId,
            countryId,
            lifetime,
            allowIdenticalRetry: false);
    }

    public AuthenticationTransportResultCode TryIssueTicketIdempotent(
        string accountName,
        string authorizationCode,
        Guid installationId,
        uint countryId,
        TimeSpan lifetime)
    {
        return TryIssueTicketCore(
            accountName,
            authorizationCode,
            installationId,
            countryId,
            lifetime,
            allowIdenticalRetry: true);
    }

    private AuthenticationTransportResultCode TryIssueTicketCore(
        string accountName,
        string authorizationCode,
        Guid installationId,
        uint countryId,
        TimeSpan lifetime,
        bool allowIdenticalRetry)
    {
        if (!TryNormalizeAuthorizationCode(
                authorizationCode,
                out string normalizedAuthorizationCode) ||
            string.IsNullOrWhiteSpace(accountName) ||
            accountName.Length > AuthenticationContractLimits.MaxAccountNameLength ||
            accountName.Any(character =>
                char.IsWhiteSpace(character) ||
                char.IsControl(character)) ||
            installationId == Guid.Empty ||
            countryId > AuthenticationContractLimits.MaxCountryId ||
            lifetime <= TimeSpan.Zero ||
            lifetime > TimeSpan.FromMinutes(10))
        {
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        lock (_ticketIssueLock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredTickets(now);

            string key = ComputeAuthorizationCodeKey(
                normalizedAuthorizationCode);
            if (_tickets.TryGetValue(key, out Ticket existingTicket))
            {
                if (allowIdenticalRetry)
                {
                    lock (existingTicket)
                    {
                        bool identicalUnconsumedTicket =
                            existingTicket.ExpiresAt > now &&
                            existingTicket.ConsumptionCount == 0 &&
                            existingTicket.SessionId <= 0 &&
                            string.Equals(
                                existingTicket.AccountName,
                                accountName,
                                StringComparison.Ordinal) &&
                            existingTicket.InstallationId == installationId &&
                            existingTicket.CountryId == countryId;
                        if (identicalUnconsumedTicket)
                        {
                            return AuthenticationTransportResultCode.Success;
                        }
                    }
                }

                return AuthenticationTransportResultCode.Conflict;
            }

            if (_tickets.Count >= MaximumOutstandingTickets)
            {
                return AuthenticationTransportResultCode.CapacityExceeded;
            }

            bool added = _tickets.TryAdd(
                key,
                new Ticket
                {
                    AccountName = accountName,
                    InstallationId = installationId,
                    CountryId = countryId,
                    ExpiresAt = now.Add(lifetime)
                });
            return added
                ? AuthenticationTransportResultCode.Success
                : AuthenticationTransportResultCode.Conflict;
        }
    }

    public AuthenticationTicketConsumptionResult TryConsumeTicket(
        string authorizationCode,
        Guid installationId,
        uint countryId,
        int proposedSessionId)
    {
        if (!TryNormalizeAuthorizationCode(
                authorizationCode,
                out string normalizedAuthorizationCode) ||
            installationId == Guid.Empty ||
            countryId > AuthenticationContractLimits.MaxCountryId ||
            proposedSessionId <= 0)
        {
            return FailedConsumption(
                AuthenticationTransportResultCode.InvalidRequest);
        }

        string key = ComputeAuthorizationCodeKey(
            normalizedAuthorizationCode);
        while (true)
        {
            if (!_tickets.TryGetValue(key, out Ticket ticket))
            {
                return FailedConsumption(
                    AuthenticationTransportResultCode.NotFoundOrExpired);
            }

            lock (ticket)
            {
                if (!_tickets.TryGetValue(key, out Ticket currentTicket) ||
                    !ReferenceEquals(currentTicket, ticket))
                {
                    continue;
                }

                DateTimeOffset now = _timeProvider.GetUtcNow();
                if (ticket.ExpiresAt <= now ||
                    ticket.InstallationId != installationId ||
                    ticket.CountryId != countryId)
                {
                    _tickets.TryRemove(key, out _);
                    return FailedConsumption(
                        AuthenticationTransportResultCode.NotFoundOrExpired);
                }

                if (ticket.SessionId <= 0)
                {
                    ticket.SessionId = proposedSessionId;
                    ticket.ExpiresAt = now.Add(MaximumActiveSessionLifetime);
                }

                if (ticket.ConsumptionCount < int.MaxValue)
                {
                    ticket.ConsumptionCount++;
                }

                return new AuthenticationTicketConsumptionResult
                {
                    Result = AuthenticationTransportResultCode.Success,
                    AccountName = ticket.AccountName,
                    ConsumptionNumber = ticket.ConsumptionCount,
                    SessionId = ticket.SessionId
                };
            }
        }
    }

    public AuthenticationTransportResultCode TryIssuePermit(
        long accountId,
        int sessionId,
        string ipAddress,
        TimeSpan lifetime)
    {
        return TryIssuePermitCore(
            accountId,
            sessionId,
            ipAddress,
            lifetime,
            allowIdenticalRetry: false);
    }

    public AuthenticationTransportResultCode TryIssuePermitIdempotent(
        long accountId,
        int sessionId,
        string ipAddress,
        TimeSpan lifetime)
    {
        return TryIssuePermitCore(
            accountId,
            sessionId,
            ipAddress,
            lifetime,
            allowIdenticalRetry: true);
    }

    private AuthenticationTransportResultCode TryIssuePermitCore(
        long accountId,
        int sessionId,
        string ipAddress,
        TimeSpan lifetime,
        bool allowIdenticalRetry)
    {
        if (accountId <= 0 ||
            sessionId <= 0 ||
            lifetime <= TimeSpan.Zero ||
            lifetime > TimeSpan.FromMinutes(10))
        {
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        lock (_permitIssueLock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredPermits(now);

            string key = BuildPermitKey(accountId, sessionId);
            string normalizedIpAddress = NormalizeIpAddress(ipAddress);
            if (_permits.TryGetValue(key, out Permit existingPermit))
            {
                if (allowIdenticalRetry &&
                    existingPermit.ExpiresAt > now &&
                    string.Equals(
                        existingPermit.IpAddress,
                        normalizedIpAddress,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return AuthenticationTransportResultCode.Success;
                }

                return AuthenticationTransportResultCode.Conflict;
            }

            if (_permits.Count >= MaximumOutstandingPermits)
            {
                return AuthenticationTransportResultCode.CapacityExceeded;
            }

            bool added = _permits.TryAdd(
                key,
                new Permit
                {
                    ExpiresAt = now.Add(lifetime),
                    IpAddress = normalizedIpAddress
                });
            return added
                ? AuthenticationTransportResultCode.Success
                : AuthenticationTransportResultCode.Conflict;
        }
    }

    public AuthenticationTransportResultCode TryConsumePermit(
        long accountId,
        int sessionId,
        string ipAddress)
    {
        if (accountId <= 0 || sessionId <= 0)
        {
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        if (!_permits.TryRemove(
                BuildPermitKey(accountId, sessionId),
                out Permit permit))
        {
            return AuthenticationTransportResultCode.NotFoundOrExpired;
        }

        string normalizedIpAddress = NormalizeIpAddress(ipAddress);
        bool accepted =
            permit.ExpiresAt > _timeProvider.GetUtcNow() &&
            (string.IsNullOrEmpty(permit.IpAddress) ||
             string.Equals(
                 permit.IpAddress,
                 normalizedIpAddress,
                 StringComparison.OrdinalIgnoreCase));
        return accepted
            ? AuthenticationTransportResultCode.Success
            : AuthenticationTransportResultCode.NotFoundOrExpired;
    }

    public AuthenticationTransportResultCode RevokePermit(
        long accountId,
        int sessionId)
    {
        if (accountId <= 0 || sessionId <= 0)
        {
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        _permits.TryRemove(BuildPermitKey(accountId, sessionId), out _);
        return AuthenticationTransportResultCode.Success;
    }

    private static AuthenticationTicketConsumptionResult FailedConsumption(
        AuthenticationTransportResultCode result)
    {
        return new AuthenticationTicketConsumptionResult
        {
            Result = result,
            AccountName = string.Empty
        };
    }

    private static bool TryNormalizeAuthorizationCode(
        string authorizationCode,
        out string normalizedAuthorizationCode)
    {
        normalizedAuthorizationCode = null;
        if (string.IsNullOrWhiteSpace(authorizationCode) ||
            authorizationCode.Length >
            AuthenticationContractLimits.MaxAuthorizationCodeLength)
        {
            return false;
        }

        if (Guid.TryParse(authorizationCode, out Guid directGuid))
        {
            normalizedAuthorizationCode = directGuid.ToString("D");
            return true;
        }

        if (authorizationCode.Length < 32 ||
            authorizationCode.Length % 2 != 0 ||
            authorizationCode.Any(character => !Uri.IsHexDigit(character)))
        {
            return false;
        }

        byte[] decodedBytes;
        try
        {
            decodedBytes = Convert.FromHexString(authorizationCode);
        }
        catch (FormatException)
        {
            return false;
        }

        string decodedText = Encoding.ASCII.GetString(decodedBytes);
        normalizedAuthorizationCode =
            Guid.TryParse(decodedText, out Guid decodedGuid)
                ? decodedGuid.ToString("D")
                : authorizationCode.ToUpperInvariant();
        return true;
    }

    private static string ComputeAuthorizationCodeKey(
        string normalizedAuthorizationCode)
    {
        byte[] hash = SHA256.HashData(
            Encoding.ASCII.GetBytes(normalizedAuthorizationCode));
        return Convert.ToBase64String(hash);
    }

    private static string BuildPermitKey(long accountId, int sessionId)
    {
        return accountId + ":" + sessionId;
    }

    private static string NormalizeIpAddress(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) &&
            !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        string value = endpoint.Trim();
        if (value.StartsWith("[", StringComparison.Ordinal))
        {
            int closingBracket = value.IndexOf(']');
            if (closingBracket > 1)
            {
                return value.Substring(1, closingBracket - 1);
            }
        }

        int lastColon = value.LastIndexOf(':');
        if (lastColon > 0 && value.IndexOf(':') == lastColon)
        {
            return value.Substring(0, lastColon);
        }

        return value;
    }

    private void RemoveExpiredTickets(DateTimeOffset now)
    {
        foreach (KeyValuePair<string, Ticket> entry in _tickets)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _tickets.TryRemove(entry.Key, out _);
            }
        }
    }

    private void RemoveExpiredPermits(DateTimeOffset now)
    {
        foreach (KeyValuePair<string, Permit> entry in _permits)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _permits.TryRemove(entry.Key, out _);
            }
        }
    }
}
