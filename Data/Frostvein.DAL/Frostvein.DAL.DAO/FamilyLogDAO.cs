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
    public class FamilyLogDAO : IFamilyLogDAO
    {
        #region Methods

        public DeleteResult Delete(long familyLogId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var famlog = context.FamilyLog.FirstOrDefault(c => c.FamilyLogId.Equals(familyLogId));

                    if (famlog != null)
                    {
                        context.FamilyLog.Remove(famlog);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), familyLogId, e.Message),
                    e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref FamilyLogDTO familyLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var FamilyLog = familyLog.FamilyLogId;
                    var entity = context.FamilyLog.FirstOrDefault(c => c.FamilyLogId.Equals(FamilyLog));

                    if (entity == null)
                    {
                        familyLog = insert(familyLog, context);
                        return SaveResult.Inserted;
                    }

                    familyLog = update(entity, familyLog, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_FAMILYLOG_ERROR"), familyLog.FamilyLogId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<FamilyLogDTO> LoadByFamilyId(long familyId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<FamilyLogDTO>();
                foreach (var familylog in context.FamilyLog.Where(fc => fc.FamilyId.Equals(familyId)))
                {
                    var dto = new FamilyLogDTO();
                    FamilyLogMapper.ToFamilyLogDTO(familylog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        private static FamilyLogDTO insert(FamilyLogDTO famlog, FrostveinContext context)
        {
            var entity = new FamilyLog();
            FamilyLogMapper.ToFamilyLog(famlog, entity);
            context.FamilyLog.Add(entity);
            context.SaveChanges();
            if (FamilyLogMapper.ToFamilyLogDTO(entity, famlog)) return famlog;

            return null;
        }

        private static FamilyLogDTO update(FamilyLog entity, FamilyLogDTO famlog, FrostveinContext context)
        {
            if (entity != null)
            {
                FamilyLogMapper.ToFamilyLog(famlog, entity);
                context.SaveChanges();
            }

            if (FamilyLogMapper.ToFamilyLogDTO(entity, famlog)) return famlog;

            return null;
        }

        #endregion
    }
}