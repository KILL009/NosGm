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

namespace Frostvein.DAL.DAO
{
    public class MinigameLogDAO : IMinigameLogDAO
    {
        #region Methods

        public SaveResult InsertOrUpdate(ref MinigameLogDTO minigameLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var minigameLogId = minigameLog.MinigameLogId;
                    var entity = context.MinigameLog.FirstOrDefault(c => c.MinigameLogId.Equals(minigameLogId));

                    if (entity == null)
                    {
                        minigameLog = insert(minigameLog, context);
                        return SaveResult.Inserted;
                    }

                    minigameLog = update(entity, minigameLog, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<MinigameLogDTO> LoadByCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    IEnumerable<MinigameLog> minigameLog =
                        context.MinigameLog.Where(a => a.CharacterId.Equals(characterId)).ToList();
                    if (minigameLog != null)
                    {
                        var result = new List<MinigameLogDTO>();
                        foreach (var input in minigameLog)
                        {
                            var dto = new MinigameLogDTO();
                            if (MinigameLogMapper.ToMinigameLogDTO(input, dto)) result.Add(dto);
                        }

                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public MinigameLogDTO LoadById(long minigameLogId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var minigameLog = context.MinigameLog.FirstOrDefault(a => a.MinigameLogId.Equals(minigameLogId));
                    if (minigameLog != null)
                    {
                        var minigameLogDTO = new MinigameLogDTO();
                        if (MinigameLogMapper.ToMinigameLogDTO(minigameLog, minigameLogDTO)) return minigameLogDTO;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        private static MinigameLogDTO insert(MinigameLogDTO account, FrostveinContext context)
        {
            var entity = new MinigameLog();
            MinigameLogMapper.ToMinigameLog(account, entity);
            context.MinigameLog.Add(entity);
            context.SaveChanges();
            MinigameLogMapper.ToMinigameLogDTO(entity, account);
            return account;
        }

        private static MinigameLogDTO update(MinigameLog entity, MinigameLogDTO account, FrostveinContext context)
        {
            if (entity != null)
            {
                MinigameLogMapper.ToMinigameLog(account, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (MinigameLogMapper.ToMinigameLogDTO(entity, account)) return account;

            return null;
        }

        #endregion
    }
}