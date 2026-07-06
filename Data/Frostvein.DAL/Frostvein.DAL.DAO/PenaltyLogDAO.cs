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
    public class PenaltyLogDAO : IPenaltyLogDAO
    {
        #region Methods

        public DeleteResult Delete(int penaltyLogId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var PenaltyLog = context.PenaltyLog.FirstOrDefault(c => c.PenaltyLogId.Equals(penaltyLogId));

                    if (PenaltyLog != null)
                    {
                        context.PenaltyLog.Remove(PenaltyLog);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("DELETE_PENALTYLOG_ERROR"), penaltyLogId,
                        e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref PenaltyLogDTO log)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var id = log.PenaltyLogId;
                    var entity = context.PenaltyLog.FirstOrDefault(c => c.PenaltyLogId.Equals(id));

                    if (entity == null)
                    {
                        log = insert(log, context);
                        return SaveResult.Inserted;
                    }

                    log = update(entity, log, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_PENALTYLOG_ERROR"), log.PenaltyLogId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<PenaltyLogDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<PenaltyLogDTO>();
                foreach (var entity in context.PenaltyLog)
                {
                    var dto = new PenaltyLogDTO();
                    PenaltyLogMapper.ToPenaltyLogDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<PenaltyLogDTO> LoadByAccount(long accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<PenaltyLogDTO>();
                foreach (var PenaltyLog in context.PenaltyLog.Where(s => s.AccountId.Equals(accountId)))
                {
                    var dto = new PenaltyLogDTO();
                    PenaltyLogMapper.ToPenaltyLogDTO(PenaltyLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public PenaltyLogDTO LoadById(int penaltyLogId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new PenaltyLogDTO();
                    if (PenaltyLogMapper.ToPenaltyLogDTO(
                        context.PenaltyLog.FirstOrDefault(s => s.PenaltyLogId.Equals(penaltyLogId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<PenaltyLogDTO> LoadByIp(string ip)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var cleanIp = ip.Replace("tcp://", "");
                cleanIp = cleanIp.Substring(0,
                    cleanIp.LastIndexOf(":") > 0 ? cleanIp.LastIndexOf(":") : cleanIp.Length);
                var result = new List<PenaltyLogDTO>();
                foreach (var PenaltyLog in context.PenaltyLog.Where(s => s.IP.Contains(cleanIp)))
                {
                    var dto = new PenaltyLogDTO();
                    PenaltyLogMapper.ToPenaltyLogDTO(PenaltyLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        private static PenaltyLogDTO insert(PenaltyLogDTO penaltylog, FrostveinContext context)
        {
            var entity = new PenaltyLog();
            PenaltyLogMapper.ToPenaltyLog(penaltylog, entity);
            context.PenaltyLog.Add(entity);
            context.SaveChanges();
            if (PenaltyLogMapper.ToPenaltyLogDTO(entity, penaltylog)) return penaltylog;

            return null;
        }

        private static PenaltyLogDTO update(PenaltyLog entity, PenaltyLogDTO penaltylog, FrostveinContext context)
        {
            if (entity != null)
            {
                PenaltyLogMapper.ToPenaltyLog(penaltylog, entity);
                context.SaveChanges();
            }

            if (PenaltyLogMapper.ToPenaltyLogDTO(entity, penaltylog)) return penaltylog;

            return null;
        }

        #endregion
    }
}