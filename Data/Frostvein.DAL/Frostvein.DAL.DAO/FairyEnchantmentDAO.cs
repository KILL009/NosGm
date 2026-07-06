using Frostvein.DAL.DAO.Generic;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class FairyEnchantmentDAO : GenericDAO<FairyEnchantmentDTO, FairyEnchantment>, IFairyEnchantmentDAO
    {
        public void DeleteByEquipmentSerialId(Guid equipmentSerialId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var remove = context.FairyEnchantment.Where(x => x.EquipmentSerialId == equipmentSerialId);
                context.FairyEnchantment.RemoveRange(remove);
                context.SaveChanges();
            }
        }

        public FairyEnchantmentDTO InsertOrUpdate(FairyEnchantmentDTO dto)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = InsertOrUpdate(context, dto, context.FairyEnchantment, new FairyEnchantmentMapper(), x => x.FairyEnchantmentId == dto.FairyEnchantmentId);
                context.SaveChanges();
                return result;
            }
        }

        public void InsertOrUpdateFromList(List<FairyEnchantmentDTO> fairyEnchantments, Guid equipmentSerialId)
        {
            if (!fairyEnchantments.Any()) return;

            using (var context = DataAccessHelper.CreateContext())
            {
                foreach (var dto in fairyEnchantments)
                {
                    dto.EquipmentSerialId = equipmentSerialId;
                    InsertOrUpdate(context, dto, context.FairyEnchantment, new FairyEnchantmentMapper(), x => x.FairyEnchantmentId == dto.FairyEnchantmentId);
                    context.SaveChanges();
                }
            }
        }

        public IEnumerable<FairyEnchantmentDTO> LoadByEquipmentSerialId(Guid id)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<FairyEnchantmentDTO>();
                foreach (var entity in context.FairyEnchantment.Where(c => c.EquipmentSerialId == id))
                {
                    var dto = new FairyEnchantmentDTO();
                    FairyEnchantmentMapper.ToDTOStatic(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }
    }
}
