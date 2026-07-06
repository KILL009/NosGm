using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.DAL.DAO
{
    public class GeneralLogDAO : IGeneralLogDAO
    {
        #region Methods


        public bool IdAlreadySet(long id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.GeneralLog.Any(gl => gl.LogId == id);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public GeneralLogDTO Insert(GeneralLogDTO generalLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new GeneralLog();
                    GeneralLogMapper.ToGeneralLog(generalLog, entity);
                    context.GeneralLog.Add(entity);
                    context.SaveChanges();
                    if (GeneralLogMapper.ToGeneralLogDTO(entity, generalLog)) return generalLog;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

       

        public SaveResult InsertOrUpdate(ref GeneralLogDTO GeneralLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var LogId = GeneralLog.LogId;
                    var entity = context.GeneralLog.FirstOrDefault(c => c.LogId.Equals(LogId));

                    if (entity == null)
                    {
                        GeneralLog = insert(GeneralLog, context);
                        return SaveResult.Inserted;
                    }

                    GeneralLog = update(entity, GeneralLog, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_GeneralLog_ERROR"), GeneralLog.LogId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<GeneralLogDTO>();
                foreach (var generalLog in context.GeneralLog)
                {
                    var dto = new GeneralLogDTO();
                    GeneralLogMapper.ToGeneralLogDTO(generalLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByAccount(long? accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<GeneralLogDTO>();
                foreach (var GeneralLog in context.GeneralLog.Where(s => s.AccountId == accountId))
                {
                    var dto = new GeneralLogDTO();
                    GeneralLogMapper.ToGeneralLogDTO(GeneralLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByIp(string ip)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var cleanIp = ip.Replace("tcp://", "");
                cleanIp = cleanIp.Substring(0,
                    cleanIp.LastIndexOf(":") > 0 ? cleanIp.LastIndexOf(":") : cleanIp.Length);
                var result = new List<GeneralLogDTO>();
                foreach (var GeneralLog in context.GeneralLog.Where(s => s.IpAddress.Contains(cleanIp)))
                {
                    var dto = new GeneralLogDTO();
                    GeneralLogMapper.ToGeneralLogDTO(GeneralLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByLogType(string logType, long? characterId, bool onlyToday = false)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<GeneralLogDTO>();
                if (onlyToday)
                {
                    DateTime today = DateTime.Now.Date;
                    foreach (var log in context.GeneralLog.Where(c => c.LogType.Equals(logType) && c.CharacterId == characterId))
                    {
                        if (log.Timestamp.Year == today.Year && log.Timestamp.Month == today.Month && log.Timestamp.Day == today.Day)
                        {
                            var dto = new GeneralLogDTO();
                            GeneralLogMapper.ToGeneralLogDTO(log, dto);
                            result.Add(dto);
                        }
                    }
                }
                else
                {
                    foreach (var log in context.GeneralLog.Where(c => c.LogType.Equals(logType) && c.CharacterId == characterId))
                    {
                        var dto = new GeneralLogDTO();
                        GeneralLogMapper.ToGeneralLogDTO(log, dto);
                        result.Add(dto);
                    }
                }
                return result;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByLogTypeAndAccountId(string logType, long? LogId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<GeneralLogDTO>();
                foreach (var log in context.GeneralLog.Where(c => c.LogType.Equals(logType) && c.LogId == LogId))
                {
                    var dto = new GeneralLogDTO();
                    GeneralLogMapper.ToGeneralLogDTO(log, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public void SetCharIdNull(long? characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    foreach (var log in context.GeneralLog.Where(c => c.CharacterId == characterId))
                        log.CharacterId = null;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void WriteGeneralLog(long LogId, string ipAddress, long? characterId, string logType, string logData)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var log = new GeneralLog
                    {
                        LogId = LogId,
                        IpAddress = ipAddress,
                        Timestamp = DateTime.Now,
                        LogType = logType,
                        LogData = logData,
                        CharacterId = characterId
                    };

                    context.GeneralLog.Add(log);
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static GeneralLogDTO insert(GeneralLogDTO GeneralLog, FrostveinContext context)
        {
            var entity = new GeneralLog();
            GeneralLogMapper.ToGeneralLog(GeneralLog, entity);
            context.GeneralLog.Add(entity);
            context.SaveChanges();
            GeneralLogMapper.ToGeneralLogDTO(entity, GeneralLog);
            return GeneralLog;
        }

        private static GeneralLogDTO update(GeneralLog entity, GeneralLogDTO GeneralLog, FrostveinContext context)
        {
            if (entity != null)
            {
                GeneralLogMapper.ToGeneralLog(GeneralLog, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (GeneralLogMapper.ToGeneralLogDTO(entity, GeneralLog)) return GeneralLog;

            return null;
        }

        private GeneralLogDTO Update(GeneralLog entity, GeneralLogDTO generalLog, FrostveinContext context)
        {
            if (entity != null)
            {
                GeneralLogMapper.ToGeneralLog(generalLog, entity);
                context.SaveChanges();
            }

            if (GeneralLogMapper.ToGeneralLogDTO(entity, generalLog)) return generalLog;

            return null;
        }

        public async Task<GeneralLogDTO> InsertAsync (GeneralLogDTO generalLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new GeneralLog();
                    GeneralLogMapper.ToGeneralLog(generalLog, entity);
                    context.GeneralLog.Add(entity);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    if (GeneralLogMapper.ToGeneralLogDTO(entity, generalLog))
                    {
                        return generalLog;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        #endregion
    }
}