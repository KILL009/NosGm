using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class SkillDAO : ISkillDAO
    {
        #region Methods

        public void Insert(List<SkillDTO> skills)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var skill in skills) InsertOrUpdate(skill);
                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public SkillDTO Insert(SkillDTO skill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Skill();
                    SkillMapper.ToSkill(skill, entity);
                    context.Skill.Add(entity);
                    context.SaveChanges();
                    if (SkillMapper.ToSkillDTO(entity, skill)) return skill;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public SaveResult InsertOrUpdate(SkillDTO skill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long SkillVNum = skill.SkillVNum;
                    var entity = context.Skill.FirstOrDefault(c => c.SkillVNum == SkillVNum);

                    if (entity == null)
                    {
                        skill = insert(skill, context);
                        return SaveResult.Inserted;
                    }

                    skill = update(entity, skill, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_SKILL_ERROR"), skill.SkillVNum,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<SkillDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<SkillDTO>();
                foreach (var Skill in context.Skill)
                {
                    var dto = new SkillDTO();
                    SkillMapper.ToSkillDTO(Skill, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public SkillDTO LoadById(short skillId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new SkillDTO();
                    if (SkillMapper.ToSkillDTO(context.Skill.FirstOrDefault(s => s.SkillVNum.Equals(skillId)), dto))
                        return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static SkillDTO insert(SkillDTO skill, FrostveinContext context)
        {
            var entity = new Skill();
            SkillMapper.ToSkill(skill, entity);
            context.Skill.Add(entity);
            context.SaveChanges();
            if (SkillMapper.ToSkillDTO(entity, skill)) return skill;

            return null;
        }

        private static SkillDTO update(Skill entity, SkillDTO skill, FrostveinContext context)
        {
            if (entity != null)
            {
                SkillMapper.ToSkill(skill, entity);
                context.SaveChanges();
            }

            if (SkillMapper.ToSkillDTO(entity, skill)) return skill;

            return null;
        }

        #endregion
    }
}