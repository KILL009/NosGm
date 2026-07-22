using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadEvents
    {
        public static void Load()
        {
            ServerManager.Instance.Schedules = ConfigurationManager.GetSection("eventScheduler") as List<Schedule>;
            ServerManager.Instance.StartedEvents = new List<EventType>();
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.Schedules.Count} Events - Status: Successful", LogType.LOAD);
        }
    }
}
