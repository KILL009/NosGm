using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
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