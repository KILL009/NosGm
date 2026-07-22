using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using NosGm.GameObject.Networking;
using NosGm.Configuration;
using static System.Collections.Specialized.BitVector32;
using NosGm.GameObject.Extension;

namespace NosGm.GameObject.Plugin.Event
{
    public static class CelestialSpire
    {
        private static readonly TimeSpan BossTime = new(0, 20, 0);
        public static void GenerateCelestialSpire(ClientSession host)
        {
            List<ClientSession> sessions = new List<ClientSession>();


            if (host.Character.Group != null)
            {
                sessions = host.Character.Group.Sessions.Where(s => s.Character.MapInstance.MapInstanceType == MapInstanceType.CelestialSpire);
            }
            else
            {
                sessions.Add(host);
            }
            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
            MapInstance map = null;
            byte stage = 1;

            //Switch stage here

            if (host.Character.HeroLevel > 30)
            {
                map = ServerManager.GenerateMapInstance(5442, MapInstanceType.CelestialSpire, new InstanceBag());
                stage = 1;
            }
            else
            {
                host.SendPacket("info You must be at least Hero Level 30 in order to enter the Celestial Spire!");
                return;
            }

            maps.Add(new Tuple<MapInstance, byte>(map, stage));

            if (map != null)
            {
                foreach (ClientSession s in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                }
            }

            foreach (Tuple<MapInstance, byte> mapinstance in maps)
            {
                CelestialSpireTask.Run(host, map);
            }
        }

        public class CelestialSpireTask
        {
            public static void Run(ClientSession Session, MapInstance mapinstance)
            {
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes), new EventContainer(mapinstance, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 10), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_MINUTES_REMAINING"), 10), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 5), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_MINUTES_REMAINING"), 5), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 3), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_MINUTES_REMAINING"), 3), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 2), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_MINUTES_REMAINING"), 2), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 1), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_MINUTES_REMAINING"), 1), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 0.5), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("CELESTIAL_SECONDS_REMAINING"), 30), 0)));
                var cancellationToken = new CancellationTokenSource();

                //Switch Stage here
                switch (Session.Character.Stage)
                {
                    case 1:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5009, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5010, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 2:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5009, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5010, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 3:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5009, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5010, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 4:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5009, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5010, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 5:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5009, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5010, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 6:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5011, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 20, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 7:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5011, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 8:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5011, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 9:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5011, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 10:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5011, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 11:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5013, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 12:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5013, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 13:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5013, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 14:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5013, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 15:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5012, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5013, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 16:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5014, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5015, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 17:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5014, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5015, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 18:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5014, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5015, 30, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 19:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5014, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5015, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 20:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5014, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5015, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 21:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5016, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5017, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 22:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5016, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5017, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 23:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5016, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5017, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 24:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5016, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5017, 35, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 25:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5016, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5017, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 26:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5018, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5019, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 27:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5018, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5019, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 28:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5018, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5019, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 29:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5018, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5019, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 30:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5018, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5019, 40, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 31:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5020, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5021, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 32:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5020, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5021, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 33:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5020, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5021, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 34:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5020, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5021, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 35:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5020, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5021, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 36:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5022, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5023, 20, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 37:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5022, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5023, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 38:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5022, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5023, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 39:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5022, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5023, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 40:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5022, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5023, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 41:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5024, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5025, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 42:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5024, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5025, 30, true, new List<EventContainer>(), false, true, false)));
                        break;
                    case 43:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5024, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5025, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 44:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5024, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5025, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 45:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 20, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 20, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 46:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 25, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 25, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 47:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 30, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 30, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 48:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 35, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 35, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 49:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 40, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 40, true, new List<EventContainer>(), false, true, false)));
                        break;

                    case 50:
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5026, 50, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5027, 50, true, new List<EventContainer>(), false, true, false)));
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(5028, 50, true, new List<EventContainer>(), false, true, false)));
                        break;
                }
                Observable.Interval(TimeSpan.FromSeconds(1)).Timeout(BossTime).Subscribe(_ =>
                {
                    if (!mapinstance.Monsters.Any(s => s.CurrentHp > 0))
                    {
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNPORTAL, new Portal { SourceX = 39, SourceY = 39, DestinationMapId = 2628, DestinationX = 151, DestinationY = 84 }));
                        Session.SendPacket("msg 4 The Stage has been cleared!");
                        int rnd = ServerManager.RandomNumber(0, 100);
                        foreach (var cli in mapinstance.Sessions.Where(s => s.Character != null).ToList())
                        {
                            cli.Character.GiftAdd(2481, cli.Character.Stage);
                            cli.Character.GiftAdd(5907, cli.Character.Stage);
                            InstanceExtension.AddBattlePassPoint(cli);
                            cli.Character.Stage++;
                            cancellationToken.Cancel();
                        }
                    }
                }, () => { EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance, EventActionType.DISPOSEMAP, null)); }, cancellationToken.Token);
            }
        }
    }
}