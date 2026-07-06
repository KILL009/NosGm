using Frostvein.Core;
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
    public class NpcMonsterSkillDAO : INpcMonsterSkillDAO
    {
        #region Methods

        public NpcMonsterSkillDTO Insert(ref NpcMonsterSkillDTO npcMonsterSkill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new NpcMonsterSkill();
                    NpcMonsterSkillMapper.ToNpcMonsterSkill(npcMonsterSkill, entity);
                    context.NpcMonsterSkill.Add(entity);
                    context.SaveChanges();
                    if (NpcMonsterSkillMapper.ToNpcMonsterSkillDTO(entity, npcMonsterSkill)) return npcMonsterSkill;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Insert(List<NpcMonsterSkillDTO> skills)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Skill in skills)
                    {
                        var entity = new NpcMonsterSkill();
                        NpcMonsterSkillMapper.ToNpcMonsterSkill(Skill, entity);
                        context.NpcMonsterSkill.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public List<NpcMonsterSkillDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<NpcMonsterSkillDTO>();
                foreach (var NpcMonsterSkillobject in context.NpcMonsterSkill)
                {
                    var dto = new NpcMonsterSkillDTO();
                    NpcMonsterSkillMapper.ToNpcMonsterSkillDTO(NpcMonsterSkillobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<NpcMonsterSkillDTO> LoadByNpcMonster(short npcId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<NpcMonsterSkillDTO>();
                foreach (var NpcMonsterSkillobject in context.NpcMonsterSkill.Where(i => i.NpcMonsterVNum == npcId))
                {
                    var dto = new NpcMonsterSkillDTO();
                    NpcMonsterSkillMapper.ToNpcMonsterSkillDTO(NpcMonsterSkillobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}