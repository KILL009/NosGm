using NosGm.Configuration;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
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
