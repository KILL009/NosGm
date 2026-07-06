using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class FamilySkillMissionDAO : IFamilySkillMissionDAO
    {
        public void DailyReset(FamilySkillMissionDTO fsm)
        {
            try
            {
                fsm.CurrentValue = (short)(fsm.ItemVNum < 9604 ? 1 : 0);
                InsertOrUpdate(ref fsm);

            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), "famskillmission", e.Message), e);
            }
        }
        public DeleteResult Delete(long itemVNum, long familyId)
        {
            try
            {
                using (FrostveinContext context = DataAccessHelper.CreateContext())
                {
                    FamilySkillMission famskillmission = context.FamilySkillMission.FirstOrDefault(c => c.ItemVNum.Equals(itemVNum) && c.FamilyId.Equals(familyId));

                    if (famskillmission != null)
                    {
                        context.FamilySkillMission.Remove(famskillmission);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), "famskillmission", e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref FamilySkillMissionDTO familySkillMission)
        {
            try
            {
                using (FrostveinContext context = DataAccessHelper.CreateContext())
                {
                    short ItemVNum = familySkillMission.ItemVNum;
                    long FamilyId = familySkillMission.FamilyId;
                    FamilySkillMission entity = context.FamilySkillMission.FirstOrDefault(c => c.ItemVNum.Equals(ItemVNum) && c.FamilyId.Equals(FamilyId));

                    if (entity == null)
                    {
                        familySkillMission = Insert(familySkillMission, context);
                        return SaveResult.Inserted;
                    }

                    familySkillMission = Update(entity, familySkillMission, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_FamilySkillMission_ERROR"), familySkillMission.FamilySkillMissionId, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IList<FamilySkillMissionDTO> LoadByFamilyId(long familyId)
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<FamilySkillMissionDTO> result = new List<FamilySkillMissionDTO>();
                foreach (FamilySkillMission entity in context.FamilySkillMission.Where(fs => fs.FamilyId.Equals(familyId)))
                {
                    FamilySkillMissionDTO dto = new FamilySkillMissionDTO();
                    Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMissionDTO(entity, dto);
                    result.Add(dto);
                }
                return result;
            }
        }
        public IEnumerable<FamilySkillMissionDTO> LoadAll()
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<FamilySkillMissionDTO> result = new List<FamilySkillMissionDTO>();
                foreach (FamilySkillMission FamilySkillMission in context.FamilySkillMission)
                {
                    FamilySkillMissionDTO dto = new FamilySkillMissionDTO();
                    Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMissionDTO(FamilySkillMission, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        private static FamilySkillMissionDTO Insert(FamilySkillMissionDTO famskillmission, FrostveinContext context)
        {
            FamilySkillMission entity = new FamilySkillMission();
            Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMission(famskillmission, entity);
            context.FamilySkillMission.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMissionDTO(entity, famskillmission))
            {
                return famskillmission;
            }

            return null;
        }

        private static FamilySkillMissionDTO Update(FamilySkillMission entity, FamilySkillMissionDTO famskillmission, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMission(famskillmission, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.FamilySkillMissionMapper.ToFamilySkillMissionDTO(entity, famskillmission))
            {
                return famskillmission;
            }

            return null;
        }
    }
}
