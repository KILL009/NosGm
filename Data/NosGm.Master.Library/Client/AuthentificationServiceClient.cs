using NosGm.Configuration;
using NosGm.Core;
using NosGm.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class AuthentificationServiceClient : IAuthentificationService
    {
        private static AuthentificationServiceClient _instance;
        private readonly IScsServiceClient<IAuthentificationService> _client;

        public AuthentificationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _client = ScsServiceClientBuilder.CreateClient<IAuthentificationService>(new ScsTcpEndPoint(ip, port));
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
            {
                try { _client.Connect(); }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"), memberName: nameof(AuthentificationServiceClient));
                    Thread.Sleep(1000);
                }
            }
        }

        public static AuthentificationServiceClient Instance => _instance ?? (_instance = new AuthentificationServiceClient());
        public CommunicationStates CommunicationState => _client.CommunicationState;
        public bool Authenticate(string authKey) => _client.ServiceProxy.Authenticate(authKey);
        public AccountDTO ValidateAccount(string userName, string passHash) => _client.ServiceProxy.ValidateAccount(userName, passHash);
        public CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash) => _client.ServiceProxy.ValidateAccountAndCharacter(userName, characterName, passHash);

        public bool RegisterGameforgeAuthTicket(string accountName, string authToken, string installationId, byte countryId)
        {
            return _client.ServiceProxy.RegisterGameforgeAuthTicket(accountName, authToken, installationId, countryId);
        }

        public string ConsumeGameforgeAuthTicket(string authToken, string installationId, byte countryId)
        {
            return _client.ServiceProxy.ConsumeGameforgeAuthTicket(authToken, installationId, countryId);
        }

        public bool RegisterGameforgeWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            return _client.ServiceProxy.RegisterGameforgeWorldPermit(accountId, sessionId, ipAddress);
        }

        public bool ConsumeGameforgeWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            return _client.ServiceProxy.ConsumeGameforgeWorldPermit(accountId, sessionId, ipAddress);
        }

        public void RevokeGameforgeWorldPermit(long accountId, int sessionId)
        {
            _client.ServiceProxy.RevokeGameforgeWorldPermit(accountId, sessionId);
        }
    }
}