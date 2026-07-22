using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface ICellonOptionDAO
    {
        #region Methods

        DeleteResult DeleteByEquipmentSerialId(Guid id);

        IEnumerable<CellonOptionDTO> GetOptionsByWearableInstanceId(Guid wearableInstanceId);

        CellonOptionDTO InsertOrUpdate(CellonOptionDTO cellonOption);

        void InsertOrUpdateFromList(List<CellonOptionDTO> cellonOption, Guid equipmentSerialId);

        Task InsertOrUpdateFromListAsync(List<CellonOptionDTO> cellonOption, Guid equipmentSerialId);

        #endregion
    }
}