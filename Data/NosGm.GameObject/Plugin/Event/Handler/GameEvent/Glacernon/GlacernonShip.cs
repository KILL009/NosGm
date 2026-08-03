using NosGm.Configuration;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Domain;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event
{
    public static class GlacernonShip
    {
        public static EventType EventType => EventType.GLACERNONSHIP;

        public static void GenerateGlacernonShip(byte faction)
        {
            if (faction != 1 && faction != 2)
            {
                Logger.Warn($"[GLACERNON_SHIP] Result=Rejected Faction={faction}");
                return;
            }

            EventHelper.Instance.RunEvent(
                new EventContainer(
                    ServerManager.GetMapInstance(
                        ServerManager.GetBaseMapInstanceIdByMapId(145)),
                    EventActionType.NPCSEFFECTCHANGESTATE,
                    true));

            DateTime nextMinute = TimeExtensions.RoundUp(
                DateTime.Now,
                TimeSpan.FromMinutes(1));
            TimeSpan delay = nextMinute - DateTime.Now;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    await GlacernonShipRuntime.RunAsync(faction).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"[GLACERNON_SHIP] Result=Failed Faction={faction}",
                        exception);
                }
            });
        }
    }

    internal static class GlacernonShipRuntime
    {
        public static async Task RunAsync(byte faction)
        {
            MapInstance map = ServerManager.GenerateMapInstance(
                149,
                faction == 1
                    ? MapInstanceType.Act4ShipAngel
                    : MapInstanceType.Act4ShipDemon,
                new InstanceBag());
            if (map == null)
            {
                throw new InvalidOperationException(
                    $"Glacernon ship map could not be created for faction {faction}.");
            }

            AddShipNpcs(map);
            Logger.Info(
                $"[GLACERNON_SHIP] Result=Started Faction={faction} " +
                $"MapInstance={map.MapInstanceId}");

            while (true)
            {
                try
                {
                    OpenShip();
                    await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                    map.Broadcast(
                        UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("SHIP_MINUTE"),
                            0));
                    await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                    map.Broadcast(
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(
                                Language.Instance.GetMessageFromKey("SHIP_SECONDS"),
                                30),
                            0));
                    await Task.Delay(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                    map.Broadcast(
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(
                                Language.Instance.GetMessageFromKey("SHIP_SECONDS"),
                                10),
                            0));
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    map.Broadcast(
                        UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("SHIP_SETOFF"),
                            0));

                    List<ClientSession> sessions = map.Sessions
                        .Where(session => session?.Character != null)
                        .ToList();
                    TeleportPlayers(sessions);
                    Logger.Info(
                        $"[GLACERNON_SHIP] Result=Departed Faction={faction} " +
                        $"Passengers={sessions.Count}");
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"[GLACERNON_SHIP] Result=CycleFailed Faction={faction}",
                        exception);
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            }
        }

        private static void AddShipNpcs(MapInstance map)
        {
            var captain = new MapNpc
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
            captain.Initialize(map);
            map.AddNPC(captain);

            var crew = new MapNpc
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
            crew.Initialize(map);
            map.AddNPC(crew);
        }

        private static void OpenShip()
        {
            EventHelper.Instance.RunEvent(
                new EventContainer(
                    ServerManager.GetMapInstance(
                        ServerManager.GetBaseMapInstanceIdByMapId(145)),
                    EventActionType.NPCSEFFECTCHANGESTATE,
                    false));
        }

        private static void TeleportPlayers(IEnumerable<ClientSession> sessions)
        {
            foreach (ClientSession session in sessions)
            {
                if (session?.Character == null)
                {
                    continue;
                }

                if (!ServerManager.Instance.IsAct4Online())
                {
                    ServerManager.Instance.ChangeMap(
                        session.Character.CharacterId,
                        145,
                        51,
                        41);
                    session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(
                            Language.Instance.GetMessageFromKey("ACT4_OFFLINE")));
                    continue;
                }

                switch (session.Character.Faction)
                {
                    case FactionType.None:
                        ServerManager.Instance.ChangeMap(
                            session.Character.CharacterId,
                            145,
                            51,
                            41);
                        session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(
                                "You need to be part of a faction to join Act 4"));
                        continue;
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
                    default:
                        continue;
                }

                session.Character.Event.EmitEvent(
                    new PlayerChangeChannelEvent(
                        ServerConfiguration.IPAddress,
                        Convert.ToInt32(ServerConfiguration.GlacernonServerPort),
                        3));
            }
        }
    }
}
