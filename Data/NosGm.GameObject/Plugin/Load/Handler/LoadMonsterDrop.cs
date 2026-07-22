using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadMonsterDrop
    {
      
        public static void Load()
        {
            ServerManager.Instance._monsterDrops = new Dictionary<short, List<DropDTO>>();
            foreach (var monsterDropGrouping in DAOFactory.DropDAO.LoadAll().GroupBy(d => d.MonsterVNum))
            {
                if (monsterDropGrouping.Key.HasValue)
                {
                    ServerManager.Instance._monsterDrops[monsterDropGrouping.Key.Value] =
                            monsterDropGrouping.OrderBy(d => d.DropChance).ToList();
                }
                else
                {
                    ServerManager.Instance._generalDrops = monsterDropGrouping.ToList();
                }
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._monsterDrops.Sum(i => i.Value.Count)} Monster Drops - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
