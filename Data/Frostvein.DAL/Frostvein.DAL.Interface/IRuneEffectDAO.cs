using Frostvein.Data;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IRuneEffectDAO
    {
        RuneEffectDTO InsertOrUpdate(RuneEffectDTO dto);
        IEnumerable<RuneEffectDTO> LoadByEquipmentSerialId(Guid id);
        void InsertOrUpdateFromList(List<RuneEffectDTO> runeEffects, Guid equipmentSerialId);
        void DeleteByEquipmentSerialId(Guid equipmentSerialId);
    }
}
