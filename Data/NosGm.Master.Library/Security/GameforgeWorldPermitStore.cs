using System;
using System.Collections.Concurrent;

namespace NosGm.Master.Library.Interface
{
    /// <summary>
    /// Carries a successful Gameforge Login authorization into World without reusing a password.
    /// Permits are short-lived, one-use and bound to account, session and client IP.
    /// </summary>
    public sealed class GameforgeWorldPermitStore
    {
        public const int MaximumOutstandingPermits = 10000;

        private sealed class Permit
        {
            public DateTime ExpiresAtUtc { get; set; }
            public string IpAddress { get; set; }
        }

        private readonly ConcurrentDictionary<string, Permit> _permits =
            new ConcurrentDictionary<string, Permit>(StringComparer.Ordinal);

        private GameforgeWorldPermitStore()
        {
        }

        public static GameforgeWorldPermitStore Instance { get; } = new GameforgeWorldPermitStore();

        public int Count => _permits.Count;

        public bool TryIssue(long accountId, int sessionId, string ipAddress, TimeSpan lifetime)
        {
            if (accountId <= 0 || sessionId <= 0 ||
                lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10))
            {
                return false;
            }

            DateTime nowUtc = DateTime.UtcNow;
            RemoveExpired(nowUtc);
            if (_permits.Count >= MaximumOutstandingPermits)
            {
                return false;
            }

            return _permits.TryAdd(BuildKey(accountId, sessionId), new Permit
            {
                ExpiresAtUtc = nowUtc.Add(lifetime),
                IpAddress = NormalizeIpAddress(ipAddress)
            });
        }

        public bool TryConsume(long accountId, int sessionId, string ipAddress)
        {
            if (accountId <= 0 || sessionId <= 0)
            {
                return false;
            }

            string key = BuildKey(accountId, sessionId);
            if (!_permits.TryRemove(key, out Permit permit))
            {
                return false;
            }

            string normalizedIp = NormalizeIpAddress(ipAddress);
            return permit.ExpiresAtUtc > DateTime.UtcNow &&
                   (string.IsNullOrEmpty(permit.IpAddress) ||
                    string.Equals(permit.IpAddress, normalizedIp, StringComparison.OrdinalIgnoreCase));
        }

        public void Revoke(long accountId, int sessionId)
        {
            _permits.TryRemove(BuildKey(accountId, sessionId), out _);
        }

        public void Clear()
        {
            _permits.Clear();
        }

        private static string BuildKey(long accountId, int sessionId)
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

        private void RemoveExpired(DateTime nowUtc)
        {
            foreach (var pair in _permits)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc)
                {
                    _permits.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}