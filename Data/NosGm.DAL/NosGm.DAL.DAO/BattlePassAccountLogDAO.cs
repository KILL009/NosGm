using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class BattlePassAccountLogDAO : IBattlePassAccountLogDAO
    {
        public bool IdAlreadySet(long id)
        {
            try
            {
                using (NosGmContext context = DataAccessHelper.CreateContext())
                {
                    return context.BattlePassAccountLog.Any(gl => gl.BpAccountLogId == id);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public DeleteResult Delete(long id)
        {
            try
            {
                using (NosGmContext context = DataAccessHelper.CreateContext())
                {
                    BattlePassAccountLog log = context.BattlePassAccountLog.FirstOrDefault(c => c.AccountId.Equals(id));

                    if (log != null)
                    {
                        context.BattlePassAccountLog.Remove(log);
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

        public SaveResult InsertOrUpdateFromList(IEnumerable<BattlePassAccountLogDTO> battlePassQuests)
        {
            try
            {
                using (NosGmContext context = DataAccessHelper.CreateContext())
                {
                    void insert(BattlePassAccountLogDTO battlePassAccountLog)
                    {
                        BattlePassAccountLog _entity = new BattlePassAccountLog();
                        Mapper.Mappers.BattlePassAccountLogMapper.ToBpAccountLog(battlePassAccountLog, _entity);
                        context.BattlePassAccountLog.Add(_entity);
                        context.SaveChanges();
                        battlePassAccountLog.BpAccountLogId = _entity.BpAccountLogId;
                    }

                    void update(BattlePassAccountLog _entity, BattlePassAccountLogDTO log)
                    {
                        if (_entity != null)
                        {
                            Mapper.Mappers.BattlePassAccountLogMapper.ToBpAccountLog(log, _entity);
                            context.SaveChanges();
                        }
                    }

                    foreach (BattlePassAccountLogDTO log in battlePassQuests)
                    {
                        BattlePassAccountLog entity = context.BattlePassAccountLog.FirstOrDefault(c => c.BpAccountLogId == log.BpAccountLogId);

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

        public IEnumerable<BattlePassAccountLogDTO> LoadAll()
        {
            using (NosGmContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassAccountLogDTO> result = new List<BattlePassAccountLogDTO>();
                foreach (BattlePassAccountLog log in context.BattlePassAccountLog)
                {
                    BattlePassAccountLogDTO dto = new BattlePassAccountLogDTO();
                    Mapper.Mappers.BattlePassAccountLogMapper.ToBpAccountLogDTO(log, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        public IEnumerable<BattlePassAccountLogDTO> LoadAllById(long id)
        {
            using (NosGmContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassAccountLogDTO> result = new List<BattlePassAccountLogDTO>();
                foreach (BattlePassAccountLog log in context.BattlePassAccountLog.Where(s => s.AccountId == id))
                {
                    BattlePassAccountLogDTO dto = new BattlePassAccountLogDTO();
                    Mapper.Mappers.BattlePassAccountLogMapper.ToBpAccountLogDTO(log, dto);
                    result.Add(dto);
                }
                return result;
            }
        }
    }
}