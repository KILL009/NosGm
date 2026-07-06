using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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