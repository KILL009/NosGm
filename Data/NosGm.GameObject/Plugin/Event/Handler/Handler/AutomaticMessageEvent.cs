using NosGm.Domain;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class AutomaticMessageEvent
    {
        public static void Publish(AutomaticMessageType messageType)
        {
            switch (messageType)
            {
                case AutomaticMessageType.GlacernonStat:
                    if (ServerManager.Instance.IsAct4Online())
                    {
                        ServerManager.Instance.Broadcast("Glacernon Stat", ReceiverType.All);
                        ServerManager.Instance.Broadcast($"msg 3 Angels: {ServerManager.Instance.Act4AngelStat.Percentage / 100}% | Demons: {ServerManager.Instance.Act4DemonStat.Percentage / 100}%", ReceiverType.All);
                    }
                    break;
            }
        }
    }
}
