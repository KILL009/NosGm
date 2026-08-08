using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using System;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    internal class ConfigurationClient : IConfigurationClient
    {
        private readonly Action<ConfigurationObject>
            _onConfigurationUpdated;

        internal ConfigurationClient(
            Action<ConfigurationObject> onConfigurationUpdated)
        {
            _onConfigurationUpdated = onConfigurationUpdated ??
                throw new ArgumentNullException(
                    nameof(onConfigurationUpdated));
        }

        #region Methods

        public void ConfigurationUpdated(ConfigurationObject configurationObject)
        {
            Task.Run(() =>
                _onConfigurationUpdated(configurationObject));
        }

        #endregion
    }
}
