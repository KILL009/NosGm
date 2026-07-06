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
    public class QuestLogDAO : IQuestLogDAO
    {
        #region Methods

        public QuestLogDTO Insert(QuestLogDTO questLog, FrostveinContext context)
        {
            try
            {
                var entity = new QuestLog();
                QuestLogMapper.ToQuestLog(questLog, entity);
                context.QuestLog.Add(entity);
                context.SaveChanges();
                if (QuestLogMapper.ToQuestLogDTO(entity, questLog)) return questLog;

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public SaveResult InsertOrUpdate(ref QuestLogDTO quest)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var questId = quest.QuestId;
                    var characterId = quest.CharacterId;
                    var entity = context.QuestLog.FirstOrDefault(c =>
                        c.QuestId.Equals(questId) && c.CharacterId.Equals(characterId));

                    if (entity == null)
                    {
                        quest = Insert(quest, context);
                        return SaveResult.Inserted;
                    }

                    quest.QuestId = entity.QuestId;
                    quest = Update(entity, quest, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<QuestLogDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<QuestLogDTO>();
                foreach (var questLog in context.QuestLog.Where(s => s.CharacterId == characterId))
                {
                    var dto = new QuestLogDTO();
                    QuestLogMapper.ToQuestLogDTO(questLog, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public QuestLogDTO LoadById(long id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new QuestLogDTO();
                    if (QuestLogMapper.ToQuestLogDTO(context.QuestLog.FirstOrDefault(i => i.Id.Equals(id)), dto))
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

        public QuestLogDTO Update(QuestLog old, QuestLogDTO replace, FrostveinContext context)
        {
            if (old != null)
            {
                QuestLogMapper.ToQuestLog(replace, old);
                context.Entry(old).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (QuestLogMapper.ToQuestLogDTO(old, replace)) return replace;

            return null;
        }

        #endregion
    }
}