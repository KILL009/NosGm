using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using Frostvein.SCS.Communication.ScsServices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace Frostvein.Master.Library.Client
{
    public class MallServiceClient : IMallService
    {
        #region Instantiation

        public MallServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _client = ScsServiceClientBuilder.CreateClient<IMallService>(new ScsTcpEndPoint(ip, port));
            Thread.Sleep(5000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: "MallServiceClient");
                    Thread.Sleep(1000);
                }
        }

        #endregion

        #region Members

        private static MallServiceClient _instance;

        private readonly IScsServiceClient<IMallService> _client;

        #endregion

        #region Properties

        public static MallServiceClient Instance => _instance ?? (_instance = new MallServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public bool Authenticate(string authKey)
        {
            return _client.ServiceProxy.Authenticate(authKey);
        }

        public IEnumerable<CharacterDTO> GetCharacters(long accountId)
        {
            return _client.ServiceProxy.GetCharacters(accountId);
        }

        public void SendItem(long characterId, MallItem item)
        {
            _client.ServiceProxy.SendItem(characterId, item);
        }

        public void SendStaticBonus(long characterId, MallStaticBonus item)
        {
            _client.ServiceProxy.SendStaticBonus(characterId, item);
        }

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            return _client.ServiceProxy.ValidateAccount(userName, passHash);
        }

        #endregion
    }
}