using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class QuestRewardDAO : IQuestRewardDAO
    {
        #region Methods

        public void Insert(List<QuestRewardDTO> questRewards)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var rewards in questRewards)
                    {
                        var entity = new QuestReward();
                        QuestRewardMapper.ToQuestReward(rewards, entity);
                        context.QuestReward.Add(entity);
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

        public QuestRewardDTO Insert(QuestRewardDTO questReward)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new QuestReward();
                    QuestRewardMapper.ToQuestReward(questReward, entity);
                    context.QuestReward.Add(entity);
                    context.SaveChanges();
                    if (QuestRewardMapper.ToQuestRewardDTO(entity, questReward)) return questReward;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public List<QuestRewardDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<QuestRewardDTO>();
                foreach (var entity in context.QuestReward)
                {
                    var dto = new QuestRewardDTO();
                    QuestRewardMapper.ToQuestRewardDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<QuestRewardDTO> LoadByQuestId(long questId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<QuestRewardDTO>();
                foreach (var reward in context.QuestReward.Where(s => s.QuestId == questId))
                {
                    var dto = new QuestRewardDTO();
                    QuestRewardMapper.ToQuestRewardDTO(reward, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}