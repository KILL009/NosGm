using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IShellEffectDAO
    {
        #region Methods

        DeleteResult DeleteByEquipmentSerialId(Guid id, bool isRune = false);

        ShellEffectDTO InsertOrUpdate(ShellEffectDTO shelleffect);

        void InsertOrUpdateFromList(List<ShellEffectDTO> shellEffects, Guid equipmentSerialId);

        Task InsertOrUpdateFromListAsync(List<ShellEffectDTO> shellEffects, Guid equipmentSerialId);

        IEnumerable<ShellEffectDTO> LoadByEquipmentSerialId(Guid id, bool isRune = false);

        #endregion
    }
}