using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using System.Threading.Tasks;

namespace Frostvein.Master.Library.Client
{
    internal class ConfigurationClient : IConfigurationClient
    {
        #region Methods

        public void ConfigurationUpdated(ConfigurationObject configurationObject)
        {
            Task.Run(() => ConfigurationServiceClient.Instance.OnConfigurationUpdated(configurationObject));
        }

        #endregion
    }
}