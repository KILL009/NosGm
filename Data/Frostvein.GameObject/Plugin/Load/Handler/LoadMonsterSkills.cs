using Frostvein.DAL;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadMonsterSkills
    {
        public static void Load()
        {
            ServerManager.Instance._monsterSkills = new Dictionary<short, List<NpcMonsterSkill>>();
            ServerManager.Instance._allMonsterSkills = new ConcurrentBag<NpcMonsterSkill>();
            DAOFactory.NpcMonsterSkillDAO.LoadAll()
                .ForEach(s => ServerManager.Instance._allMonsterSkills.Add(new NpcMonsterSkill(s)));
            foreach (var monsterSkillGrouping in DAOFactory.NpcMonsterSkillDAO.LoadAll().ToArray()
                .GroupBy(n => n.NpcMonsterVNum))
            {
                ServerManager.Instance._monsterSkills[monsterSkillGrouping.Key] =
                        monsterSkillGrouping.Select(n => new NpcMonsterSkill(n)).ToList();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._monsterSkills.Sum(i => i.Value.Count)} Monster Drops - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
