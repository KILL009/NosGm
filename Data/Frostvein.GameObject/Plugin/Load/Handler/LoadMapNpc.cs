using Frostvein.DAL;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadMapNpc
    {

        public static void Load()
        {
            ServerManager.Instance._mapNpcs = new Dictionary<short, List<MapNpc>>();
            var npcs = DAOFactory.MapNpcDAO.LoadAll().GroupBy(t => t.MapId);
            foreach (var mapNpcGrouping in npcs)
            {
                ServerManager.Instance._mapNpcs[mapNpcGrouping.Key] = mapNpcGrouping.Select(t => t as MapNpc).ToList();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._mapNpcs.Sum(i => i.Value.Count)} Map NPCs - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
