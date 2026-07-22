using NosGm.Master.Library.Data;

namespace NosGm.Master.Library.Interface
{
    public interface IConfigurationClient
    {
        #region Methods

        void ConfigurationUpdated(ConfigurationObject configurationObject);

        #endregion
    }
}