using NosGm.Configuration;
using NosGm.Core;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    internal interface IConfigurationRollbackTransport
    {
        bool Authenticate(string authKey, Guid serverId);

        ConfigurationObject GetConfigurationObject();

        void UpdateConfigurationObject(
            ConfigurationObject configurationObject);
    }

    internal static class ConfigurationRollbackTransportFactory
    {
        public static IConfigurationRollbackTransport Create(
            Action<ConfigurationObject> onConfigurationUpdated)
        {
            return new ScsConfigurationRollbackTransport(
                onConfigurationUpdated);
        }
    }

    internal sealed class ScsConfigurationRollbackTransport
        : IConfigurationRollbackTransport
    {
        private readonly IScsServiceClient<IConfigurationService> _client;

        public ScsConfigurationRollbackTransport(
            Action<ConfigurationObject> onConfigurationUpdated)
        {
            if (onConfigurationUpdated == null)
            {
                throw new ArgumentNullException(
                    nameof(onConfigurationUpdated));
            }

            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(
                ServerConfiguration.MasterServerPort);
            var callback = new ConfigurationClient(
                onConfigurationUpdated);
            _client = ScsServiceClientBuilder
                .CreateClient<IConfigurationService>(
                    new ScsTcpEndPoint(ip, port),
                    callback);

            Thread.Sleep(1000);
            while (_client.CommunicationState !=
                   CommunicationStates.Connected)
            {
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(
                        Language.Instance.GetMessageFromKey(
                            "RETRY_CONNECTION"),
                        memberName:
                            nameof(ScsConfigurationRollbackTransport));
                    Thread.Sleep(1000);
                }
            }
        }

        public bool Authenticate(string authKey, Guid serverId)
        {
            return _client.ServiceProxy.Authenticate(authKey, serverId);
        }

        public ConfigurationObject GetConfigurationObject()
        {
            return _client.ServiceProxy.GetConfigurationObject();
        }

        public void UpdateConfigurationObject(
            ConfigurationObject configurationObject)
        {
            _client.ServiceProxy.UpdateConfigurationObject(
                configurationObject);
        }
    }
}
