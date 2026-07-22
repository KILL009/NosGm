using NosGm.Core;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadProperty
    {
        public static void Load()
        {
            ServerManager.Instance.Act4RaidStart = DateTime.Now;
            ServerManager.Instance.Act4AngelStat = new Act4Stat();
            ServerManager.Instance.Act4DemonStat = new Act4Stat();
            ServerManager.Instance.Act6Erenia = new Act4Stat();
            ServerManager.Instance.Act6Zenas = new Act4Stat();
            ServerManager.Instance.LastFCSent = DateTime.Now;
            ServerManager.Instance.CharacterScreenSessions = new ThreadSafeSortedList<long, ClientSession>();
        }
    }
}
