using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Master.Library.Interface
{
    [Serializable]
    public sealed class GameforgeAuthTicketConsumption
    {
        public string AccountName { get; set; }
        public int ConsumptionNumber { get; set; }
        public int SessionId { get; set; }
        public bool IsFirstConsumption => ConsumptionNumber == 1;
    }

    public sealed class GameforgeAuthTicketStore
    {
        public const int MaximumOutstandingTickets = 10000;
        public const int MaximumConsumptionsPerTicket = 3;

        private sealed class Ticket
        {
            public string AccountName { get; set; }
            public Guid InstallationId { get; set; }
            public byte CountryId { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public int RemainingConsumptions { get; set; }
            public int SessionId { get; set; }
        }

        private readonly ConcurrentDictionary<string, Ticket> _tickets = new ConcurrentDictionary<string, Ticket>(StringComparer.Ordinal);
        private GameforgeAuthTicketStore() { }
        public static GameforgeAuthTicketStore Instance { get; } = new GameforgeAuthTicketStore();
        public int Count => _tickets.Count;

        public bool TryIssue(string accountName, string authToken, Guid installationId, byte countryId, TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(accountName) || accountName.Length > 255 ||
                accountName.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v', '\0' }) >= 0 ||
                !GameforgeLoginPacketParser.TryNormalizeAuthToken(authToken, out string normalizedToken) ||
                installationId == Guid.Empty || countryId > GameforgeLoginPacketParser.MaximumCountryId ||
                lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10)) return false;

            DateTime nowUtc = DateTime.UtcNow;
            RemoveExpired(nowUtc);
            if (_tickets.Count >= MaximumOutstandingTickets) return false;
            string key = ComputeTokenKey(normalizedToken);
            return _tickets.TryAdd(key, new Ticket
            {
                AccountName = accountName,
                InstallationId = installationId,
                CountryId = countryId,
                ExpiresAtUtc = nowUtc.Add(lifetime),
                RemainingConsumptions = MaximumConsumptionsPerTicket
            });
        }

        public bool TryConsume(
            string authToken,
            Guid installationId,
            byte countryId,
            int proposedSessionId,
            out GameforgeAuthTicketConsumption consumption)
        {
            consumption = null;
            if (!GameforgeLoginPacketParser.TryNormalizeAuthToken(authToken, out string normalizedToken) ||
                installationId == Guid.Empty || countryId > GameforgeLoginPacketParser.MaximumCountryId ||
                proposedSessionId <= 0) return false;

            string key = ComputeTokenKey(normalizedToken);
            while (true)
            {
                if (!_tickets.TryGetValue(key, out Ticket ticket)) return false;

                lock (ticket)
                {
                    if (!_tickets.TryGetValue(key, out Ticket currentTicket) || !ReferenceEquals(currentTicket, ticket))
                    {
                        continue;
                    }

                    if (ticket.ExpiresAtUtc <= DateTime.UtcNow ||
                        ticket.InstallationId != installationId ||
                        ticket.CountryId != countryId ||
                        ticket.RemainingConsumptions <= 0)
                    {
                        _tickets.TryRemove(key, out _);
                        return false;
                    }

                    if (ticket.SessionId <= 0) ticket.SessionId = proposedSessionId;
                    int consumptionNumber = MaximumConsumptionsPerTicket - ticket.RemainingConsumptions + 1;
                    ticket.RemainingConsumptions--;
                    consumption = new GameforgeAuthTicketConsumption
                    {
                        AccountName = ticket.AccountName,
                        ConsumptionNumber = consumptionNumber,
                        SessionId = ticket.SessionId
                    };
                    if (ticket.RemainingConsumptions == 0)
                    {
                        _tickets.TryRemove(key, out _);
                    }
                    return true;
                }
            }
        }

        public void Clear() => _tickets.Clear();

        private static string ComputeTokenKey(string normalizedToken)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.ASCII.GetBytes(normalizedToken)));
            }
        }

        private void RemoveExpired(DateTime nowUtc)
        {
            foreach (var pair in _tickets)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc) _tickets.TryRemove(pair.Key, out _);
            }
        }
    }

    public sealed class GameforgeWorldPermitStore
    {
        public const int MaximumOutstandingPermits = 10000;

        private sealed class Permit
        {
            public DateTime ExpiresAtUtc { get; set; }
            public string IpAddress { get; set; }
        }

        private readonly ConcurrentDictionary<string, Permit> _permits = new ConcurrentDictionary<string, Permit>(StringComparer.Ordinal);
        private GameforgeWorldPermitStore() { }
        public static GameforgeWorldPermitStore Instance { get; } = new GameforgeWorldPermitStore();
        public int Count => _permits.Count;

        public bool TryIssue(long accountId, int sessionId, string ipAddress, TimeSpan lifetime)
        {
            if (accountId <= 0 || sessionId <= 0 || lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10)) return false;
            DateTime nowUtc = DateTime.UtcNow;
            RemoveExpired(nowUtc);
            if (_permits.Count >= MaximumOutstandingPermits) return false;
            return _permits.TryAdd(BuildKey(accountId, sessionId), new Permit
            {
                ExpiresAtUtc = nowUtc.Add(lifetime),
                IpAddress = NormalizeIpAddress(ipAddress)
            });
        }

        public bool TryConsume(long accountId, int sessionId, string ipAddress)
        {
            if (accountId <= 0 || sessionId <= 0) return false;
            if (!_permits.TryRemove(BuildKey(accountId, sessionId), out Permit permit)) return false;
            string normalizedIp = NormalizeIpAddress(ipAddress);
            return permit.ExpiresAtUtc > DateTime.UtcNow &&
                   (string.IsNullOrEmpty(permit.IpAddress) || string.Equals(permit.IpAddress, normalizedIp, StringComparison.OrdinalIgnoreCase));
        }

        public void Revoke(long accountId, int sessionId) => _permits.TryRemove(BuildKey(accountId, sessionId), out _);
        public void Clear() => _permits.Clear();
        private static string BuildKey(long accountId, int sessionId) => accountId + ":" + sessionId;

        private static string NormalizeIpAddress(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return string.Empty;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Host)) return uri.Host;
            string value = endpoint.Trim();
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                int closingBracket = value.IndexOf(']');
                if (closingBracket > 1) return value.Substring(1, closingBracket - 1);
            }
            int lastColon = value.LastIndexOf(':');
            if (lastColon > 0 && value.IndexOf(':') == lastColon) return value.Substring(0, lastColon);
            return value;
        }

        private void RemoveExpired(DateTime nowUtc)
        {
            foreach (var pair in _permits)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc) _permits.TryRemove(pair.Key, out _);
            }
        }
    }
}
