using NosGm.Data;
using NosGm.SCS.Communication.ScsServices.Service;

namespace NosGm.Master.Library.Interface
{
    [ScsService(Version = "1.2.0.0")]
    public interface IAuthentificationService
    {
        /// <summary>
        /// Authenticates a trusted NosGM service client.
        /// </summary>
        bool Authenticate(string authKey);

        AccountDTO ValidateAccount(string userName, string passHash);

        CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash);

        /// <summary>
        /// Registers a short-lived, one-use ticket obtained by an external authentication bridge.
        /// The raw token is hashed immediately and is never stored by Master.
        /// </summary>
        bool RegisterGameforgeAuthTicket(
            string accountName,
            string authToken,
            string installationId,
            byte countryId);

        /// <summary>
        ///     Stores a short-lived one-time code issued by the trusted AuthServer.
        /// </summary>
        bool StoreModernLoginTicket(string authCode, string accountName, string ipAddress);

        /// <summary>
        ///     Resolves and consumes a NoS0576/NoS0577 authentication token.
        /// </summary>
        string ConsumeModernLoginTicket(string authToken, string ipAddress);

        /// <summary>
        ///     Registers the temporary World-entry permission created by a modern login.
        /// </summary>
        bool RegisterModernLoginSession(long accountId, int sessionId, string ipAddress);

        /// <summary>
        ///     Consumes the temporary World-entry permission for a modern login.
        /// </summary>
        bool ConsumeModernLoginSession(long accountId, int sessionId, string ipAddress);

        /// <summary>
        ///     Revokes a temporary modern login permission when Login cannot finish.
        /// </summary>
        void RevokeModernLoginSession(long accountId, int sessionId);

        #endregion
        /// Atomically consumes a previously registered ticket. Returns the local account name
        /// only when token, installation and country all match.
        /// </summary>
        string ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId);
    }
}
