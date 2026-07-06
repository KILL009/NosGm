using Frostvein.Data;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IFairyEnchantmentDAO
    {
        FairyEnchantmentDTO InsertOrUpdate(FairyEnchantmentDTO dto);
        IEnumerable<FairyEnchantmentDTO> LoadByEquipmentSerialId(Guid id);
        void InsertOrUpdateFromList(List<FairyEnchantmentDTO> fairyEnchantments, Guid equipmentSerialId);
        void DeleteByEquipmentSerialId(Guid equipmentSerialId);
    }
}
