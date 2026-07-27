using NosGm.Data;
using NosGm.SCS.Communication.ScsServices.Service;

namespace NosGm.Master.Library.Interface
{
    [ScsService(Version = "1.1.0.0")]
    public interface IAuthentificationService
    {
        #region Methods

        /// <summary>
        ///     Authenticates a Client to the Service
        /// </summary>
        /// <param name="authKey">The private Authentication key</param>
        /// <returns>true if successful, else false</returns>
        bool Authenticate(string authKey);

        /// <summary>
        ///     Checks if the given Credentials are Valid
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passHash"></param>
        /// <returns></returns>
        AccountDTO ValidateAccount(string userName, string passHash);

        /// <summary>
        ///     Checks if the given Credentials are Valid and return the CharacterDTO
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="characterName"></param>
        /// <param name="passHash"></param>
        /// <returns></returns>
        CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash);

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
    }
}