using NosGm.Data;
using NosGm.SCS.Communication.ScsServices.Service;

namespace NosGm.Master.Library.Interface
{
    [ScsService(Version = "1.2.0.0")]
    public interface IAuthentificationService
    {
        bool Authenticate(string authKey);
        AccountDTO ValidateAccount(string userName, string passHash);
        CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash);

        bool RegisterGameforgeAuthTicket(string accountName, string authToken, string installationId, byte countryId);
        string ConsumeGameforgeAuthTicket(string authToken, string installationId, byte countryId);
        bool RegisterGameforgeWorldPermit(long accountId, int sessionId, string ipAddress);
        bool ConsumeGameforgeWorldPermit(long accountId, int sessionId, string ipAddress);
        void RevokeGameforgeWorldPermit(long accountId, int sessionId);
    }
}