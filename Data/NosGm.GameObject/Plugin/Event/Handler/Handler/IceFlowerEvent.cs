using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class IceFlowerEvent
    {
        public static void Load()
        {
            foreach (var map in ServerManager.GetAllMapInstances().Where(s =>
                s.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4) &&
                s.Npcs.Count(o => o.NpcVNum == 2004 && o.IsOut) < s.Npcs.Count(n => n.NpcVNum == 2004)))
                foreach (var i in map.Npcs.Where(s => s.IsOut && s.NpcVNum == 2004))
                {
                    var randomPos = map.Map.GetRandomPosition();
                    i.MapX = randomPos.X;
                    i.MapY = randomPos.Y;
                    i.MapInstance.Broadcast(i.GenerateIn());
                }
        }
    }
}
