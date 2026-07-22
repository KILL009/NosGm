using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class QuestObjectiveDAO : IQuestObjectiveDAO
    {
        #region Methods

        public void Insert(List<QuestObjectiveDTO> quests)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var quest in quests)
                    {
                        var entity = new QuestObjective();
                        QuestObjectiveMapper.ToQuestObjective(quest, entity);
                        context.QuestObjective.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public QuestObjectiveDTO Insert(QuestObjectiveDTO questObjective)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new QuestObjective();
                    QuestObjectiveMapper.ToQuestObjective(questObjective, entity);
                    context.QuestObjective.Add(entity);
                    context.SaveChanges();
                    if (QuestObjectiveMapper.ToQuestObjectiveDTO(entity, questObjective)) return questObjective;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public List<QuestObjectiveDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<QuestObjectiveDTO>();
                foreach (var questObjective in context.QuestObjective)
                {
                    var dto = new QuestObjectiveDTO();
                    QuestObjectiveMapper.ToQuestObjectiveDTO(questObjective, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<QuestObjectiveDTO> LoadByQuestId(long questId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<QuestObjectiveDTO>();
                foreach (var questObjective in context.QuestObjective.Where(s => s.QuestId == questId))
                {
                    var dto = new QuestObjectiveDTO();
                    QuestObjectiveMapper.ToQuestObjectiveDTO(questObjective, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}