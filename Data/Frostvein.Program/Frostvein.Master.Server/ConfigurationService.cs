using Frostvein.Configuration;
using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;
using System.Configuration;

namespace Frostvein.Master.Server
{
    internal class ConfigurationService : ScsService, IConfigurationService
    {
        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            if (string.IsNullOrWhiteSpace(authKey)) return false;

            if (authKey == ServerConfiguration.MasterAuthKey)
            {
                MSManager.Instance.AuthentificatedClients.Add(CurrentClient.ClientId);

                var ws = MSManager.Instance.WorldServers.Find(s => s.Id == serverId);
                if (ws != null) ws.ConfigurationServiceClient = CurrentClient;
                return true;
            }

            return false;
        }

        public ConfigurationObject GetConfigurationObject()
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;
            return MSManager.Instance.ConfigurationObject;
        }

        public void UpdateConfigurationObject(ConfigurationObject configurationObject)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;
            MSManager.Instance.ConfigurationObject = configurationObject;

            foreach (var ws in MSManager.Instance.WorldServers)
                ws.ConfigurationServiceClient.GetClientProxy<IConfigurationClient>()
                    .ConfigurationUpdated(MSManager.Instance.ConfigurationObject);
        }

        #endregion
    }
}