using NosGm.Data;
using System;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IRuneEffectDAO
    {
        RuneEffectDTO InsertOrUpdate(RuneEffectDTO dto);
        IEnumerable<RuneEffectDTO> LoadByEquipmentSerialId(Guid id);
        void InsertOrUpdateFromList(List<RuneEffectDTO> runeEffects, Guid equipmentSerialId);
        void DeleteByEquipmentSerialId(Guid equipmentSerialId);
    }
}
