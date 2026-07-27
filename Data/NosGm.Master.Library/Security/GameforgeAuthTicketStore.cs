using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Master.Library.Interface
{
    /// <summary>
    /// Thread-safe, in-memory ticket store. Tokens are kept only as SHA-256 lookup keys,
    /// consumed atomically, bound to InstallationId and CountryId, and expire quickly.
    /// </summary>
    public sealed class GameforgeAuthTicketStore
    {
        public const int MaximumOutstandingTickets = 10000;

        private sealed class Ticket
        {
            public string AccountName { get; set; }
            public Guid InstallationId { get; set; }
            public byte CountryId { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }

        private readonly ConcurrentDictionary<string, Ticket> _tickets =
            new ConcurrentDictionary<string, Ticket>(StringComparer.Ordinal);

        private GameforgeAuthTicketStore()
        {
        }

        public static GameforgeAuthTicketStore Instance { get; } = new GameforgeAuthTicketStore();

        public int Count => _tickets.Count;

        public bool TryIssue(
            string accountName,
            string authToken,
            Guid installationId,
            byte countryId,
            TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(accountName) || accountName.Length > 255 ||
                accountName.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v', '\0' }) >= 0 ||
                !GameforgeLoginPacketParser.IsSupportedAuthToken(authToken) ||
                installationId == Guid.Empty ||
                countryId > GameforgeLoginPacketParser.MaximumCountryId ||
                lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10))
            {
                return false;
            }

            DateTime nowUtc = DateTime.UtcNow;
            RemoveExpired(nowUtc);
            if (_tickets.Count >= MaximumOutstandingTickets)
            {
                return false;
            }

            string key = ComputeTokenKey(authToken);
            return _tickets.TryAdd(key, new Ticket
            {
                AccountName = accountName,
                InstallationId = installationId,
                CountryId = countryId,
                ExpiresAtUtc = nowUtc.Add(lifetime)
            });
        }

        public bool TryConsume(
            string authToken,
            Guid installationId,
            byte countryId,
            out string accountName)
        {
            accountName = null;
            if (!GameforgeLoginPacketParser.IsSupportedAuthToken(authToken) ||
                installationId == Guid.Empty ||
                countryId > GameforgeLoginPacketParser.MaximumCountryId)
            {
                return false;
            }

            string key = ComputeTokenKey(authToken);
            if (!_tickets.TryRemove(key, out Ticket ticket))
            {
                return false;
            }

            if (ticket.ExpiresAtUtc <= DateTime.UtcNow ||
                ticket.InstallationId != installationId ||
                ticket.CountryId != countryId)
            {
                return false;
            }

            accountName = ticket.AccountName;
            return true;
        }

        public void Clear()
        {
            _tickets.Clear();
        }

        private static string ComputeTokenKey(string authToken)
        {
            string normalized;
            if (Guid.TryParse(authToken, out Guid guid))
            {
                normalized = guid.ToString("D");
            }
            else
            {
                normalized = authToken.ToUpperInvariant();
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.ASCII.GetBytes(normalized)));
            }
        }

        private void RemoveExpired(DateTime nowUtc)
        {
            foreach (var pair in _tickets)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc)
                {
                    _tickets.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}
