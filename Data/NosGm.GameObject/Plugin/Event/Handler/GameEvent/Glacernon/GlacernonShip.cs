using NosGm.Configuration;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Domain;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event
{
    public class GlacernonShip
    {
        #region Methods

        public static EventType EventType => EventType.GLACERNONSHIP;

        public static void GenerateGlacernonShip(byte faction)
        {
            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(145)), EventActionType.NPCSEFFECTCHANGESTATE, true));
            var result = TimeExtensions.RoundUp(DateTime.Now, TimeSpan.FromMinutes(1));
            Observable.Timer(result - DateTime.Now).Subscribe(X => GlacernonShipThread.Run(faction));
        }

        #endregion
    }

    public static class GlacernonShipThread
    {
        #region Methods

        public static void Run(byte faction)
        {
            var map = ServerManager.GenerateMapInstance(149, faction == 1 ? MapInstanceType.Act4ShipAngel : MapInstanceType.Act4ShipDemon, new InstanceBag());
            if (map == null)
            {
                return;
            }

            var mapNpc1 = new MapNpc
            {
                NpcVNum = 613,
                MapNpcId = map.GetNextNpcId(),
                Dialog = 434,
                MapId = 177,
                MapX = 76,
                MapY = 127,
                IsMoving = false,
                Position = 1,
                IsSitting = false
            };
            mapNpc1.Initialize(map);
            map.AddNPC(mapNpc1);
            var mapNpc2 = new MapNpc
            {
                NpcVNum = 540,
                MapNpcId = map.GetNextNpcId(),
                Dialog = 433,
                MapId = 177,
                MapX = 76,
                MapY = 127,
                IsMoving = false,
                Position = 3,
                IsSitting = false
            };
            mapNpc2.Initialize(map);
            map.AddNPC(mapNpc2);
            while (true)
            {
                openShip();
                Thread.Sleep(60 * 1000);
                map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHIP_MINUTE"), 0));
                // lockShip();
                Thread.Sleep(30 * 1000);
                map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SHIP_SECONDS"), 30), 0));
                Thread.Sleep(20 * 1000);
                map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SHIP_SECONDS"), 10), 0));
                Thread.Sleep(10 * 1000);
                map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHIP_SETOFF"), 0));
                var sessions = map.Sessions.Where(s => s?.Character != null).ToList();
                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(X => teleportPlayers(sessions));
            }
        }
        private static void openShip()
        {
            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(145)), EventActionType.NPCSEFFECTCHANGESTATE, false));
        }

        private static void teleportPlayers(List<ClientSession> sessions)
        {
            foreach (var session in sessions)
            {
                if (ServerManager.Instance.IsAct4Online())
                {
                    switch (session.Character.Faction)
                    {
                        case FactionType.None:
                            ServerManager.Instance.ChangeMap(session.Character.CharacterId, 145, 51, 41);
                            session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to be part of a faction to join Act 4"));
                            return;

                        case FactionType.Angel:
                            session.Character.MapId = 130;
                            session.Character.MapX = 12;
                            session.Character.MapY = 40;
                            break;

                        case FactionType.Demon:
                            session.Character.MapId = 131;
                            session.Character.MapX = 12;
                            session.Character.MapY = 40;
                            break;
                    }

                    session.Character.Event.EmitEvent(new PlayerChangeChannelEvent(ServerConfiguration.IPAddress, Convert.ToInt32(ServerConfiguration.GlacernonServerPort), 3));
                }
                else
                {
                    ServerManager.Instance.ChangeMap(session.Character.CharacterId, 145, 51, 41); //145 51 41
                    session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ACT4_OFFLINE")));
                }
            }
        }

        #endregion
    }
}