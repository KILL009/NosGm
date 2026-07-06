using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class BattlePassQuestProgressDAO : IBattlePassQuestProgressDAO
    {
        public DeleteResult Delete(long id)
        {
            try
            {
                using (FrostveinContext context = DataAccessHelper.CreateContext())
                {
                    BattlePassQuestProgress log = context.BattlePassQuestProgress.FirstOrDefault(c => c.BpQuestProgressId.Equals(id));

                    if (log != null)
                    {
                        context.BattlePassQuestProgress.Remove(log);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ACCOUNT_ERROR"), id, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdateFromList(IEnumerable<BattlePassQuestProgressDTO> battlePassQuests)
        {
            try
            {
                using (FrostveinContext context = DataAccessHelper.CreateContext())
                {
                    void insert(BattlePassQuestProgressDTO battlePassAccountLog)
                    {
                        BattlePassQuestProgress _entity = new BattlePassQuestProgress();
                        Mapper.Mappers.BattlePassQuestProgressMapper.ToBpQuestProgress(battlePassAccountLog, _entity);
                        context.BattlePassQuestProgress.Add(_entity);
                        context.SaveChanges();
                        battlePassAccountLog.BpQuestProgressId = _entity.BpQuestProgressId;
                    }

                    void update(BattlePassQuestProgress _entity, BattlePassQuestProgressDTO log)
                    {
                        if (_entity != null)
                        {
                            Mapper.Mappers.BattlePassQuestProgressMapper.ToBpQuestProgress(log, _entity);
                            context.SaveChanges();
                        }
                    }

                    foreach (BattlePassQuestProgressDTO log in battlePassQuests)
                    {
                        BattlePassQuestProgress entity = context.BattlePassQuestProgress.FirstOrDefault(c => c.BpQuestProgressId == log.BpQuestProgressId);

                        if (entity == null)
                        {
                            insert(log);
                        }
                        else
                        {
                            update(entity, log);
                        }
                    }

                    context.SaveChanges();
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<BattlePassQuestProgressDTO> LoadAll()
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassQuestProgressDTO> result = new List<BattlePassQuestProgressDTO>();

                foreach (var prize in context.BattlePassQuestProgress)
                {
                    BattlePassQuestProgressDTO dto = new BattlePassQuestProgressDTO();
                    Mapper.Mappers.BattlePassQuestProgressMapper.ToBpQuestProgressDTO(prize, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        public IEnumerable<BattlePassQuestProgressDTO> LoadByAccountId(long id)
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassQuestProgressDTO> result = new List<BattlePassQuestProgressDTO>();
                foreach (BattlePassQuestProgress log in context.BattlePassQuestProgress.Where(s => s.AccountId == id))
                {
                    BattlePassQuestProgressDTO dto = new BattlePassQuestProgressDTO();
                    Mapper.Mappers.BattlePassQuestProgressMapper.ToBpQuestProgressDTO(log, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        public IEnumerable<BattlePassQuestProgressDTO> LoadByType(BpTimeType type)
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassQuestProgressDTO> result = new List<BattlePassQuestProgressDTO>();
                foreach (BattlePassQuestProgress progress in context.BattlePassQuestProgress.Where(b => b.BattlePassQuest.BpTimeType == type))
                {
                    BattlePassQuestProgressDTO dto = new BattlePassQuestProgressDTO();
                    Mapper.Mappers.BattlePassQuestProgressMapper.ToBpQuestProgressDTO(progress, dto);
                    result.Add(dto);
                }

                return result;
            }
        }
    }
}