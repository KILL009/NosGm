using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadQuest
    {
        public static void Load()
        {
            ServerManager.Instance.Quests = new List<Quest>();
            var questRewards = DAOFactory.QuestRewardDAO.LoadAll();
            var questObjectives = DAOFactory.QuestObjectiveDAO.LoadAll();
            foreach (var questdto in DAOFactory.QuestDAO.LoadAll().ToArray())
            {
                var quest = new Quest(questdto);
                quest.QuestRewards = questRewards.Where(s => s.QuestId == quest.QuestId).ToList();
                quest.QuestObjectives = questObjectives.Where(s => s.QuestId == quest.QuestId).ToList();
                ServerManager.Instance.Quests.Add(quest);
            }

            ServerManager.Instance.FlowerQuestId = ServerManager.Instance.Quests.Find(q => q.QuestType == (byte)QuestType.FlowerQuest)?.QuestId;

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.Quests.Count} Quests - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
