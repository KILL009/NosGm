using Frostvein.Master.Library.Data;

namespace Frostvein.Master.Library.Interface
{
    public interface IConfigurationClient
    {
        #region Methods

        void ConfigurationUpdated(ConfigurationObject configurationObject);

        #endregion
    }
}