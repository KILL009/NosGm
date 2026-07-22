using NosGm.DAL.DAO.Generic;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class RuneEffectDAO : GenericDAO<RuneEffectDTO, RuneEffect>, IRuneEffectDAO
    {
        public void DeleteByEquipmentSerialId(Guid equipmentSerialId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var remove = context.RuneEffects.Where(x => x.EquipmentSerialId == equipmentSerialId);
                context.RuneEffects.RemoveRange(remove);
                context.SaveChanges();
            }
        }

        public RuneEffectDTO InsertOrUpdate(RuneEffectDTO dto)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = InsertOrUpdate(context, dto, context.RuneEffects, new RuneEffectMapper(), x => x.RuneEffectId == dto.RuneEffectId);
                context.SaveChanges();
                return result;
            }
        }

        public void InsertOrUpdateFromList(List<RuneEffectDTO> runeEffects, Guid equipmentSerialId)
        {
            if (!runeEffects.Any()) return;

            using (var context = DataAccessHelper.CreateContext())
            {
                foreach (var dto in runeEffects)
                {
                    dto.EquipmentSerialId = equipmentSerialId;
                    InsertOrUpdate(context, dto, context.RuneEffects, new RuneEffectMapper(), x => x.RuneEffectId == dto.RuneEffectId);
                    context.SaveChanges();
                }
            }
        }

        public IEnumerable<RuneEffectDTO> LoadByEquipmentSerialId(Guid id)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RuneEffectDTO>();
                foreach (var entity in context.RuneEffects.Where(c => c.EquipmentSerialId == id))
                {
                    var dto = new RuneEffectDTO();
                    RuneEffectMapper.ToDTOStatic(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }
    }
}
