using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using Frostvein.SCS.Communication.ScsServices.Client;
using System;
using System.Configuration;
using System.Threading;

namespace Frostvein.Master.Library.Client
{
    public class ConfigurationServiceClient : IConfigurationService
    {
        #region Instantiation

        public ConfigurationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _confClient = new ConfigurationClient();
            _client = ScsServiceClientBuilder.CreateClient<IConfigurationService>(new ScsTcpEndPoint(ip, port),
                _confClient);
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: nameof(CommunicationServiceClient));
                    Thread.Sleep(1000);
                }
        }

        #endregion

        #region Events

        public event EventHandler ConfigurationUpdate;

        #endregion

        #region Members

        private static ConfigurationServiceClient _instance;

        private readonly IScsServiceClient<IConfigurationService> _client;

        private readonly ConfigurationClient _confClient;

        #endregion

        #region Properties

        public static ConfigurationServiceClient Instance =>
            _instance ?? (_instance = new ConfigurationServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            return _client.ServiceProxy.Authenticate(authKey, serverId);
        }

        public ConfigurationObject GetConfigurationObject()
        {
            return _client.ServiceProxy.GetConfigurationObject();
        }

        public void UpdateConfigurationObject(ConfigurationObject configurationObject)
        {
            _client.ServiceProxy.UpdateConfigurationObject(configurationObject);
        }

        internal void OnConfigurationUpdated(ConfigurationObject configurationObject)
        {
            ConfigurationUpdate?.Invoke(configurationObject, null);
        }

        #endregion
    }
}