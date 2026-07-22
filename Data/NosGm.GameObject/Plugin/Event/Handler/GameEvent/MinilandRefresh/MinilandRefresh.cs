using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public class MinilandRefresh
    {
        public static void GenerateMinilandEvent()
        {
            ServerManager.Instance.StartedEvents.Remove(EventType.MINILANDREFRESHEVENT);
        }
    }
}
