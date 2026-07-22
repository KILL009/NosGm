using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace NosGm.GameObject.Event
{
    public static class PTS
    {
        #region Methods

        public static void GeneratePTS(int Vnum, ClientSession host)
        {
            List<ClientSession> sessions = new List<ClientSession>();
            if (host.Character.Group != null)
            {
                sessions = host.Character.Group.Sessions.Where(s => s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);
            }
            else
            {
                sessions.Add(host);
            }
            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
            MapInstance map = null;
            byte instancelevel = 1;

            if (host.Character.Level < 20)
            {
                map = ServerManager.GenerateMapInstance(2100, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 15;
            }
            if (host.Character.Level > 19 && host.Character.Level < 30)
            {
                map = ServerManager.GenerateMapInstance(2103, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 25;
            }
            if (host.Character.Level > 29 && host.Character.Level < 46)
            {
                map = ServerManager.GenerateMapInstance(4214, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 35;
            }
            if (host.Character.Level > 45 && host.Character.Level < 55)
            {
                map = ServerManager.GenerateMapInstance(4311, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 45;
            }
            if (host.Character.Level > 54 && host.Character.Level < 65)
            {
                map = ServerManager.GenerateMapInstance(4503, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 55;
            }
            if (host.Character.Level > 64 && host.Character.Level < 75)
            {
                map = ServerManager.GenerateMapInstance(2105, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 65;
            }
            if (host.Character.Level > 74 && host.Character.Level < 80)
            {
                map = ServerManager.GenerateMapInstance(4643, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 75;
            }
            if (host.Character.Level > 79 && host.Character.Level < 88)
            {
                map = ServerManager.GenerateMapInstance(2101, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 80;
            }
            if (host.Character.Level > 87)
            {
                map = ServerManager.GenerateMapInstance(5417, MapInstanceType.TimeSpaceInstance, new InstanceBag());
                instancelevel = 88;
            }
            maps.Add(new Tuple<MapInstance, byte>(map, instancelevel));

            if (map != null)
            {
                foreach (ClientSession s in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                }
            }

            foreach (Tuple<MapInstance, byte> mapinstance in maps)
            {
                PSTTask task = new PSTTask();
                Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => PSTTask.Run(mapinstance));
            }
        }

        #endregion

        #region Classes

        public class PSTTask
        {
            #region Methods

            public static void Run(Tuple<MapInstance, byte> mapinstance)
            {
                long maxGold = GameConfiguration.MaxGold;
                Thread.Sleep(10 * 1000);
                Observable.Timer(TimeSpan.FromMinutes(2.6)).Subscribe(X =>
                {
                    for (int d = 0; d < 60; d++)
                    {
                        if (!mapinstance.Item1.Monsters.Any(s => s.CurrentHp > 0))
                        {
                            EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SPAWNPORTAL, new Portal { SourceX = 33, SourceY = 34, DestinationMapId = 1 }));
                            mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_SUCCEEDED"), 0));
                            foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                            {
                                cli.Character.SpAdditionPoint = cli.Character.SpAdditionPoint > 20000 ? 20000 : cli.Character.SpAdditionPoint;
                                cli.SendPacket(cli.Character.GenerateSpPoint());
                                cli.SendPacket(cli.Character.GenerateGold());
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_FXP"), 25), 10));
                            }
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                });

                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(4), new EventContainer(mapinstance.Item1, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(1), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_MINUTES_REMAINING"), 2), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_MINUTES_REMAINING"), 1), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_MONSTERS_INCOMING"), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_MONSTERS_HERE"), 0)));

                for (int wave = 0; wave < 3; wave++)
                {
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10 + (wave * 60)), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getInstantBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave)));
                }
            }


            private static List<MonsterToSummon> getInstantBattleMonster(Map map, short instantbattletype, int wave)
            {
                List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();

                switch (instantbattletype)
                {
                    case 15:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 25:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 35:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 45:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 55:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 65:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 75:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(1244, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1123, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1252, 15, false, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(1187, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1191, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1195, 15, false, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(1016, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1017, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1018, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1019, 1, false, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 80:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(1131, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1132, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1133, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1134, 10, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(1167, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1168, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1169, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1170, 10, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(1019, 10, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 88:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(237, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(246, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(248, 10, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(248, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(246, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(234, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(221, 10, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(225, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(221, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(207, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(234, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                }
                return SummonParameters;
            }

            #endregion
        }

        #endregion
    }
}