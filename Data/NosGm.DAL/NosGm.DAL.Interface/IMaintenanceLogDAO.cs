using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IMaintenanceLogDAO
    {
        #region Methods

        MaintenanceLogDTO Insert(MaintenanceLogDTO maintenanceLog);

        IEnumerable<MaintenanceLogDTO> LoadAll();

        MaintenanceLogDTO LoadFirst();

        #endregion
    }
}