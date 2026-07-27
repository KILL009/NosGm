using NosGm.Configuration;
using NosGm.Core;
using NosGm.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Configuration;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class AuthentificationServiceClient : IAuthentificationService
    {
        #region Instantiation

        public AuthentificationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _client = ScsServiceClientBuilder.CreateClient<IAuthentificationService>(new ScsTcpEndPoint(ip, port));
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: nameof(AuthentificationServiceClient));
                    Thread.Sleep(1000);
                }
        }

        #endregion

        #region Members

        private static AuthentificationServiceClient _instance;

        private readonly IScsServiceClient<IAuthentificationService> _client;

        #endregion

        #region Properties

        public static AuthentificationServiceClient Instance =>
            _instance ?? (_instance = new AuthentificationServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public bool Authenticate(string authKey)
        {
            return _client.ServiceProxy.Authenticate(authKey);
        }

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            return _client.ServiceProxy.ValidateAccount(userName, passHash);
        }

        public CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash)
        {
            return _client.ServiceProxy.ValidateAccountAndCharacter(userName, characterName, passHash);
        }

        public bool StoreModernLoginTicket(string authCode, string accountName, string ipAddress)
        {
            return _client.ServiceProxy.StoreModernLoginTicket(authCode, accountName, ipAddress);
        }

        public string ConsumeModernLoginTicket(string authToken, string ipAddress)
        {
            return _client.ServiceProxy.ConsumeModernLoginTicket(authToken, ipAddress);
        }

        public bool RegisterModernLoginSession(long accountId, int sessionId, string ipAddress)
        {
            return _client.ServiceProxy.RegisterModernLoginSession(accountId, sessionId, ipAddress);
        }

        public bool ConsumeModernLoginSession(long accountId, int sessionId, string ipAddress)
        {
            return _client.ServiceProxy.ConsumeModernLoginSession(accountId, sessionId, ipAddress);
        }

        public void RevokeModernLoginSession(long accountId, int sessionId)
        {
            _client.ServiceProxy.RevokeModernLoginSession(accountId, sessionId);
        }

        #endregion
    }
}