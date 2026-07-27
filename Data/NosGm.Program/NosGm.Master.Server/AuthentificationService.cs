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
        public bool Authenticate(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                return false;
            }

            if (authKey == ServerConfiguration.AuthServiceKey)
            {
                MSManager.Instance.AuthentificatedClients.Add(CurrentClient.ClientId);
                return true;
            }

            return false;
        }

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passHash))
            {
                return null;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(userName);
            return account?.Password == passHash ? account : null;
        }

        public CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash)
        {
            if (!IsAuthenticatedClient() || string.IsNullOrEmpty(userName) ||
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
            if (!IsAuthenticatedClient() ||
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
            if (!IsAuthenticatedClient() ||
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

        private bool IsAuthenticatedClient()
        {
            return MSManager.Instance.AuthentificatedClients.Any(
                clientId => clientId.Equals(CurrentClient.ClientId));
        }
    }
}
