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
        /// Atomically consumes a previously registered ticket. Returns the local account name
        /// only when token, installation and country all match.
        /// </summary>
        string ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId);
    }
}
