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
    public static class PluginLoadTeleporter
    {
        public static void Load()
        {
            ServerManager.Instance._teleporters = new Dictionary<int, List<TeleporterDTO>>();
            foreach (var teleporterGrouping in DAOFactory.TeleporterDAO.LoadAll().GroupBy(t => t.MapNpcId))
            {
                ServerManager.Instance._teleporters[teleporterGrouping.Key] = teleporterGrouping.Select(t => t).ToList();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._teleporters.Sum(i => i.Value.Count)} Teleporter - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
