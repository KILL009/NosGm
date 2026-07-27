using NosGm.Configuration;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Linq;

namespace NosGm.Master.Server
{
    internal class AuthentificationService : ScsService, IAuthentificationService
    {
        private const int MinimumGameforgeKeyLength = 32;

        public bool Authenticate(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                return false;
            }

            long clientId = CurrentClient.ClientId;
            if (string.Equals(authKey, ServerConfiguration.AuthServiceKey, StringComparison.Ordinal))
            {
                AddClientOnce(MSManager.Instance.AuthenticationServiceClients, clientId);
                return true;
            }

            if (!ServerConfiguration.EnableGameforgeTokenLogin ||
                !HasSecureGameforgeKeys())
            {
                return false;
            }

            if (string.Equals(
                    authKey,
                    ServerConfiguration.GameforgeTicketIssuerKey,
                    StringComparison.Ordinal))
            {
                AddClientOnce(MSManager.Instance.GameforgeTicketIssuerClients, clientId);
                return true;
            }

            if (string.Equals(
                    authKey,
                    ServerConfiguration.GameforgeTicketConsumerKey,
                    StringComparison.Ordinal))
            {
                AddClientOnce(MSManager.Instance.GameforgeTicketConsumerClients, clientId);
                return true;
            }

            return false;
        }

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            if (!IsLegacyAuthClient() || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passHash))
            {
                return null;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(userName);
            return account?.Password == passHash ? account : null;
        }

        public CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash)
        {
            if (!IsLegacyAuthClient() || string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(passHash))
            {
                return null;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(userName);
            if (account?.Password != passHash)
            {
                return null;
            }

            CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(characterName);
            return character?.AccountId == account.AccountId ? character : null;
        }

        public bool RegisterGameforgeAuthTicket(
            string accountName,
            string authToken,
            string installationId,
            byte countryId)
        {
            if (!ServerConfiguration.EnableGameforgeTokenLogin ||
                !IsGameforgeIssuerClient() ||
                !Guid.TryParse(installationId, out Guid parsedInstallationId) ||
                !GameforgeLoginPacketParser.TryGetCulture(countryId, out _))
            {
                return false;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(accountName);
            if (account == null || !string.Equals(account.Name, accountName, StringComparison.Ordinal))
            {
                return false;
            }

            int configuredTtl = ServerConfiguration.GameforgeAuthTicketTtlSeconds;
            int ttlSeconds = Math.Max(15, Math.Min(600, configuredTtl));
            return GameforgeAuthTicketStore.Instance.TryIssue(
                account.Name,
                authToken,
                parsedInstallationId,
                countryId,
                TimeSpan.FromSeconds(ttlSeconds));
        }

        public string ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId)
        {
            if (!ServerConfiguration.EnableGameforgeTokenLogin ||
                !IsGameforgeConsumerClient() ||
                !Guid.TryParse(installationId, out Guid parsedInstallationId))
            {
                return null;
            }

            return GameforgeAuthTicketStore.Instance.TryConsume(
                authToken,
                parsedInstallationId,
                countryId,
                out string accountName)
                ? accountName
                : null;
        }

        public bool StoreModernLoginTicket(string authCode, string accountName, string ipAddress)
        {
            if (!IsAuthenticatedClient() || string.IsNullOrWhiteSpace(accountName))
            {
                return false;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(accountName);
            if (account == null || !string.Equals(account.Name, accountName, StringComparison.Ordinal))
            {
                return false;
            }

            return ModernLoginTicketStore.StoreTicket(authCode, account.Name, ipAddress);
        }

        public string ConsumeModernLoginTicket(string authToken, string ipAddress)
        {
            return IsAuthenticatedClient()
                ? ModernLoginTicketStore.ConsumeTicket(authToken, ipAddress)
                : null;
        }

        public bool RegisterModernLoginSession(long accountId, int sessionId, string ipAddress)
        {
            return IsAuthenticatedClient() &&
                   ModernLoginTicketStore.RegisterWorldPermit(accountId, sessionId, ipAddress);
        }

        public bool ConsumeModernLoginSession(long accountId, int sessionId, string ipAddress)
        {
            return IsAuthenticatedClient() &&
                   ModernLoginTicketStore.ConsumeWorldPermit(accountId, sessionId, ipAddress);
        }

        public void RevokeModernLoginSession(long accountId, int sessionId)
        {
            if (IsAuthenticatedClient())
            {
                ModernLoginTicketStore.RevokeWorldPermit(accountId, sessionId);
            }
        }

        private bool IsAuthenticatedClient()
        {
            return MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId));
        }

        #endregion
    }

    internal static class ModernLoginTicketStore
    {
        private sealed class LoginTicket
        {
            public string AccountName { get; set; }

            public DateTime ExpiresUtc { get; set; }

            public string IpAddress { get; set; }
        }

        private sealed class WorldPermit
        {
            public DateTime ExpiresUtc { get; set; }

            public string IpAddress { get; set; }
        }

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, LoginTicket> LoginTickets =
            new Dictionary<string, LoginTicket>(StringComparer.Ordinal);
        private static readonly Dictionary<string, WorldPermit> WorldPermits =
            new Dictionary<string, WorldPermit>(StringComparer.Ordinal);
        private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan WorldPermitLifetime = TimeSpan.FromMinutes(2);

        public static bool StoreTicket(string authCode, string accountName, string ipAddress)
        {
            string normalizedCode = NormalizeSecret(authCode);
            if (normalizedCode == null || string.IsNullOrWhiteSpace(accountName))
            {
                return false;
            }

            lock (SyncRoot)
            {
                CleanupExpired(DateTime.UtcNow);
                LoginTickets[normalizedCode] = new LoginTicket
                {
                    AccountName = accountName,
                    ExpiresUtc = DateTime.UtcNow.Add(TicketLifetime),
                    IpAddress = NormalizeIpAddress(ipAddress)
                };
            }

            return true;
        }

        public static string ConsumeTicket(string authToken, string ipAddress)
        {
            string normalizedToken = NormalizeSecret(authToken);
            if (normalizedToken == null)
            {
                return null;
            }

            string normalizedIp = NormalizeIpAddress(ipAddress);
            lock (SyncRoot)
            {
                DateTime now = DateTime.UtcNow;
                CleanupExpired(now);

                string accountName = TryConsumeTicket(normalizedToken, normalizedIp, now);
                if (accountName != null)
                {
                    return accountName;
                }

                if (TryDecodeHexAscii(normalizedToken, out string decodedCode))
                {
                    string normalizedCode = NormalizeSecret(decodedCode);
                    if (normalizedCode != null &&
                        !string.Equals(normalizedCode, normalizedToken, StringComparison.Ordinal))
                    {
                        return TryConsumeTicket(normalizedCode, normalizedIp, now);
                    }
                }
            }

            return null;
        }

        public static bool RegisterWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            if (accountId <= 0 || sessionId <= 0)
            {
                return false;
            }

            lock (SyncRoot)
            {
                CleanupExpired(DateTime.UtcNow);
                WorldPermits[BuildSessionKey(accountId, sessionId)] = new WorldPermit
                {
                    ExpiresUtc = DateTime.UtcNow.Add(WorldPermitLifetime),
                    IpAddress = NormalizeIpAddress(ipAddress)
                };
            }

            return true;
        }

        public static bool ConsumeWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            string key = BuildSessionKey(accountId, sessionId);
            string normalizedIp = NormalizeIpAddress(ipAddress);

            lock (SyncRoot)
            {
                DateTime now = DateTime.UtcNow;
                CleanupExpired(now);
                if (!WorldPermits.TryGetValue(key, out WorldPermit permit) ||
                    permit.ExpiresUtc <= now ||
                    !IpMatches(permit.IpAddress, normalizedIp))
                {
                    return false;
                }

                WorldPermits.Remove(key);
                return true;
            }
        }

        public static void RevokeWorldPermit(long accountId, int sessionId)
        {
            lock (SyncRoot)
            {
                WorldPermits.Remove(BuildSessionKey(accountId, sessionId));
            }
        }

        private static string TryConsumeTicket(string code, string ipAddress, DateTime now)
        {
            if (!LoginTickets.TryGetValue(code, out LoginTicket ticket) ||
                ticket.ExpiresUtc <= now ||
                !IpMatches(ticket.IpAddress, ipAddress))
            {
                return null;
            }

            LoginTickets.Remove(code);
            return ticket.AccountName;
        }

        private static void CleanupExpired(DateTime now)
        {
            foreach (string key in LoginTickets
                .Where(entry => entry.Value.ExpiresUtc <= now)
                .Select(entry => entry.Key)
                .ToArray())
            {
                LoginTickets.Remove(key);
            }

            foreach (string key in WorldPermits
                .Where(entry => entry.Value.ExpiresUtc <= now)
                .Select(entry => entry.Key)
                .ToArray())
            {
                WorldPermits.Remove(key);
            }
        }

        private static bool IpMatches(string expected, string actual)
        {
            return string.IsNullOrEmpty(expected) ||
                   string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSessionKey(long accountId, int sessionId)
        {
            return accountId + ":" + sessionId;
        }

        private static string NormalizeSecret(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim();
            if (normalized.Length < 16 || normalized.Length > 1024)
            {
                return null;
            }

            foreach (char character in normalized)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                {
                    return null;
                }
            }

            return Guid.TryParse(normalized, out Guid guid)
                ? guid.ToString("D")
                : normalized;
        }

        private static bool TryDecodeHexAscii(string value, out string decoded)
        {
            decoded = null;
            if (string.IsNullOrEmpty(value) || value.Length % 2 != 0 || value.Length > 2048)
            {
                return false;
            }

            var bytes = new byte[value.Length / 2];
            for (int i = 0; i < value.Length; i += 2)
            {
                if (!byte.TryParse(
                    value.Substring(i, 2),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out bytes[i / 2]))
                {
                    return false;
                }
            }

            decoded = Encoding.UTF8.GetString(bytes);
            return !string.IsNullOrWhiteSpace(decoded);
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
    }
}
        private static void AddClientOnce(ThreadSafeGenericLockedList<long> clients, long clientId)
        {
            if (!clients.Any(existingId => existingId.Equals(clientId)))
            {
                clients.Add(clientId);
            }
        }

        private static bool HasSecureGameforgeKeys()
        {
            string issuerKey = ServerConfiguration.GameforgeTicketIssuerKey;
            string consumerKey = ServerConfiguration.GameforgeTicketConsumerKey;
            return IsSecureGameforgeKey(issuerKey) &&
                   IsSecureGameforgeKey(consumerKey) &&
                   !string.Equals(issuerKey, consumerKey, StringComparison.Ordinal) &&
                   !string.Equals(issuerKey, ServerConfiguration.AuthServiceKey, StringComparison.Ordinal) &&
                   !string.Equals(consumerKey, ServerConfiguration.AuthServiceKey, StringComparison.Ordinal);
        }

        private static bool IsSecureGameforgeKey(string configuredKey)
        {
            return !string.IsNullOrWhiteSpace(configuredKey) &&
                   configuredKey.Length >= MinimumGameforgeKeyLength;
        }

        private bool IsLegacyAuthClient()
        {
            return MSManager.Instance.AuthenticationServiceClients.Any(
                clientId => clientId.Equals(CurrentClient.ClientId));
        }

        private bool IsGameforgeIssuerClient()
        {
            return MSManager.Instance.GameforgeTicketIssuerClients.Any(
                clientId => clientId.Equals(CurrentClient.ClientId));
        }

        private bool IsGameforgeConsumerClient()
        {
            return MSManager.Instance.GameforgeTicketConsumerClients.Any(
                clientId => clientId.Equals(CurrentClient.ClientId));
        }
    }
}
