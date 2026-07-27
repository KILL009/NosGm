using NosGm.Data;
using NosGm.SCS.Communication.ScsServices.Service;

namespace NosGm.Master.Library.Interface
{
    [ScsService(Version = "1.2.0.0")]
    public interface IAuthentificationService
    {
        #region Methods

        bool Authenticate(string authKey);

        AccountDTO ValidateAccount(string userName, string passHash);

        CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash);

        /// <summary>
        /// Registers a short-lived, one-use ticket obtained by a trusted external authentication bridge.
        /// </summary>
        bool RegisterGameforgeAuthTicket(
            string accountName,
            string authToken,
            string installationId,
            byte countryId);

        /// <summary>
        /// Atomically consumes a registered ticket after token, installation and country validation.
        /// </summary>
        string ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId);

        /// <summary>
        /// Registers the one-use authorization carried from Login into World.
        /// </summary>
        bool RegisterGameforgeWorldPermit(long accountId, int sessionId, string ipAddress);

        /// <summary>
        /// Consumes the one-use World authorization for the matching account, session and IP.
        /// </summary>
        bool ConsumeGameforgeWorldPermit(long accountId, int sessionId, string ipAddress);

        /// <summary>
        /// Revokes a World permit when Login cannot complete the server-list response.
        /// </summary>
        void RevokeGameforgeWorldPermit(long accountId, int sessionId);

        #endregion
    }
}