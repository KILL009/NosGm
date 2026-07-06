using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Event.Handler
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
