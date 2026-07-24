using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class GeneralLogDAO : IGeneralLogDAO
    {
        #region Methods

        public bool Enqueue(GeneralLogDTO generalLog)
        {
            if (generalLog == null)
            {
                return false;
            }

            EnsureTimestamp(generalLog);
            if (GeneralLogBatchWriter.TryEnqueue(generalLog))
            {
                return true;
            }

            return Insert(generalLog) != null;
        }

        public bool ExistsForAccount(long accountId, string logData, DateTime fromInclusive, DateTime toExclusive)
        {
            if (string.IsNullOrWhiteSpace(logData) || toExclusive <= fromInclusive)
            {
                return false;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.GeneralLog.AsNoTracking().Any(log =>
                        log.AccountId == accountId &&
                        log.LogData == logData &&
                        log.Timestamp >= fromInclusive &&
                        log.Timestamp < toExclusive);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public bool FlushPending(TimeSpan timeout) => GeneralLogBatchWriter.Flush(timeout);

        public bool IdAlreadySet(long id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.GeneralLog.AsNoTracking().Any(gl => gl.LogId == id);
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
            if (generalLog == null)
            {
                return null;
            }

            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    EnsureTimestamp(generalLog);
                    var entity = new GeneralLog();
                    GeneralLogMapper.ToGeneralLog(generalLog, entity);
                    context.GeneralLog.Add(entity);
                    context.SaveChanges();

                    success = GeneralLogMapper.ToGeneralLogDTO(entity, generalLog);
                    return success ? generalLog : null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                stopwatch.Stop();
                LogPipelineMonitor.RecordGeneralLogWrite(1, stopwatch.ElapsedTicks, success);
            }
        }

        [Obsolete("GeneralLog is append-only. Use Insert for new log records.")]
        public SaveResult InsertOrUpdate(ref GeneralLogDTO generalLog)
        {
            if (generalLog == null)
            {
                return SaveResult.Error;
            }

            if (generalLog.LogId <= 0)
            {
                GeneralLogDTO inserted = Insert(generalLog);
                if (inserted == null)
                {
                    return SaveResult.Error;
                }

                generalLog = inserted;
                return SaveResult.Inserted;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long logId = generalLog.LogId;
                    GeneralLog entity = context.GeneralLog.FirstOrDefault(log => log.LogId == logId);
                    if (entity == null)
                    {
                        return SaveResult.Error;
                    }

                    EnsureTimestamp(generalLog);
                    GeneralLogMapper.ToGeneralLog(generalLog, entity);
                    context.Entry(entity).State = EntityState.Modified;
                    context.SaveChanges();
                    GeneralLogMapper.ToGeneralLogDTO(entity, generalLog);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_GeneralLog_ERROR"), generalLog.LogId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<GeneralLogDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking())
                    .OrderBy(log => log.LogId)
                    .ToList();
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByAccount(long? accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking().Where(log => log.AccountId == accountId))
                    .OrderBy(log => log.LogId)
                    .ToList();
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return new List<GeneralLogDTO>();
            }

            string cleanIp = ip.Replace("tcp://", string.Empty);
            int separatorIndex = cleanIp.LastIndexOf(':');
            if (separatorIndex > 0)
            {
                cleanIp = cleanIp.Substring(0, separatorIndex);
            }

            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking()
                        .Where(log => log.IpAddress != null && log.IpAddress.Contains(cleanIp)))
                    .OrderByDescending(log => log.Timestamp)
                    .ToList();
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByLogType(string logType, long? characterId, bool onlyToday = false)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                IQueryable<GeneralLog> query = context.GeneralLog.AsNoTracking()
                    .Where(log => log.LogType == logType && log.CharacterId == characterId);

                if (onlyToday)
                {
                    DateTime start = DateTime.Now.Date;
                    DateTime end = start.AddDays(1);
                    query = query.Where(log => log.Timestamp >= start && log.Timestamp < end);
                }

                return Project(query)
                    .OrderBy(log => log.Timestamp)
                    .ThenBy(log => log.LogId)
                    .ToList();
            }
        }

        public IEnumerable<GeneralLogDTO> LoadByLogTypeAndAccountId(string logType, long? accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking()
                        .Where(log => log.LogType == logType && log.AccountId == accountId))
                    .OrderByDescending(log => log.Timestamp)
                    .ThenByDescending(log => log.LogId)
                    .ToList();
            }
        }

        public GeneralLogDTO LoadLatestByAccountAndType(long accountId, string logType)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking()
                        .Where(log => log.AccountId == accountId && log.LogType == logType))
                    .OrderByDescending(log => log.Timestamp)
                    .ThenByDescending(log => log.LogId)
                    .FirstOrDefault();
            }
        }

        public GeneralLogDTO LoadLatestByType(string logType, long? characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking()
                        .Where(log => log.LogType == logType && log.CharacterId == characterId))
                    .OrderByDescending(log => log.Timestamp)
                    .ThenByDescending(log => log.LogId)
                    .FirstOrDefault();
            }
        }

        public IEnumerable<GeneralLogDTO> LoadRecentByAccount(long accountId, int take)
        {
            int safeTake = Math.Max(1, Math.Min(500, take));
            using (var context = DataAccessHelper.CreateContext())
            {
                return Project(context.GeneralLog.AsNoTracking().Where(log => log.AccountId == accountId))
                    .OrderByDescending(log => log.Timestamp)
                    .ThenByDescending(log => log.LogId)
                    .Take(safeTake)
                    .ToList();
            }
        }

        public void SetCharIdNull(long? characterId)
        {
            if (!characterId.HasValue)
            {
                return;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Database.ExecuteSqlCommand(
                        "UPDATE dbo.GeneralLog SET CharacterId = NULL WHERE CharacterId = @p0",
                        characterId.Value);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void WriteGeneralLog(long accountId, string ipAddress, long? characterId, string logType,
            string logData)
        {
            Enqueue(new GeneralLogDTO
            {
                AccountId = accountId,
                CharacterId = characterId,
                IpAddress = ipAddress,
                LogType = logType,
                LogData = logData,
                Timestamp = DateTime.Now
            });
        }

        public async Task<GeneralLogDTO> InsertAsync(GeneralLogDTO generalLog)
        {
            if (generalLog == null)
            {
                return null;
            }

            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    EnsureTimestamp(generalLog);
                    var entity = new GeneralLog();
                    GeneralLogMapper.ToGeneralLog(generalLog, entity);
                    context.GeneralLog.Add(entity);
                    await context.SaveChangesAsync().ConfigureAwait(false);

                    success = GeneralLogMapper.ToGeneralLogDTO(entity, generalLog);
                    return success ? generalLog : null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                stopwatch.Stop();
                LogPipelineMonitor.RecordGeneralLogWrite(1, stopwatch.ElapsedTicks, success);
            }
        }

        private static void EnsureTimestamp(GeneralLogDTO generalLog)
        {
            if (generalLog.Timestamp == default(DateTime))
            {
                generalLog.Timestamp = DateTime.Now;
            }
        }

        private static IQueryable<GeneralLogDTO> Project(IQueryable<GeneralLog> query)
        {
            return query.Select(log => new GeneralLogDTO
            {
                AccountId = log.AccountId,
                CharacterId = log.CharacterId,
                IpAddress = log.IpAddress,
                LogData = log.LogData,
                LogId = log.LogId,
                LogType = log.LogType,
                Timestamp = log.Timestamp
            });
        }

        #endregion
    }
}
