using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.GameObject.Extension.Message;
using NosGm.Configuration.GameEvent;
using System.Collections.Concurrent;
using System.Threading;

namespace NosGm.GameObject.Plugin.Event
{
    public class AsgobasInstantBattle
    {

        public static void SendMessageWithDelay(string Message, TimeSpan delay)
        {
            ServerManager.Instance.Broadcast("msg 6 " + Message, ReceiverType.All);
            Thread.Sleep(delay);
        }

        public static void GenerateInstantBattle()
        {
            SendMessageWithDelay("Asgobas Instant Battle will start in 5 Minute(s)", TimeSpan.FromMinutes(5));
            SendMessageWithDelay("Asgobas Instant Battle will start in 1 Minute(s)", TimeSpan.FromMinutes(1));
            SendMessageWithDelay("Asgobas Instant Battle will start in 30 Seconds", TimeSpan.FromSeconds(30));
            SendMessageWithDelay("Asgobas Instant Battle will start in 10 Seconds", TimeSpan.FromSeconds(20));
            ServerManager.Instance.Sessions.Where(s => s.Character?.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList().ForEach(s => s.SendPacket($"qnamli 51 #guri^596 2547 0 0 0"));
            ServerManager.Instance.EventInWaiting = true;
            SendMessageWithDelay("Asgobas Instant Battle started", TimeSpan.FromSeconds(30));
            ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == false).ToList().ForEach(s => s.SendPacket("esf"));
            ServerManager.Instance.EventInWaiting = false;
            List<ClientSession> sessions = ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList();
            Dictionary<byte, byte> levelDictionary = new Dictionary<byte, byte>
                {
                    {1, 39},
                    {40, 49},
                    {50, 59},
                    {60, 69},
                    {70, 79},
                    {80, 99}
                };

            foreach (var kvp in levelDictionary)
            {
                var toAddSessions = sessions.Where(s => s.Character.Level >= kvp.Key && s.Character.HeroLevel >= 30 && s.Character.Level <= kvp.Value && !s.Character.IsMuted()).ToList();
                CreateInstantBattleMaps(toAddSessions, kvp.Key);
            }

            ServerManager.Instance.Sessions.Where(s => s.Character != null).ToList().ForEach(s => s.Character.IsWaitingForEvent = false);
            ServerManager.Instance.StartedEvents.Remove(EventType.ASGOBAS);
            foreach (var mapInstance in from mapInstance in Maps let task = new InstantBattleTask() select mapInstance)
            {
                Observable.Start(() => InstantBattleTask.Run(mapInstance));
            }
        }

        private static void CreateInstantBattleMaps(List<ClientSession> sessions, byte instanceLevel)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return;
            }
            sessions = sessions.Shuffle();
            var currentPlaceInList = 0;
            MapInstance map = ServerManager.GenerateMapInstance(2004, MapInstanceType.NormalInstance, new InstanceBag());
            Maps.Add(new Tuple<MapInstance, byte>(map, instanceLevel));

            foreach (var session in sessions)
            {
                if (session?.Character == null)
                {
                    continue;
                }

                if (currentPlaceInList % 50 == 0)
                {
                    map = ServerManager.GenerateMapInstance(2717, MapInstanceType.NormalInstance, new InstanceBag());
                    Maps.Add(new Tuple<MapInstance, byte>(map, instanceLevel));
                }

                ServerManager.Instance.TeleportOnRandomPlaceInMap(session, map.MapInstanceId);
                currentPlaceInList++;
            }
        }

        private static readonly List<Tuple<MapInstance, byte>> Maps = new List<Tuple<MapInstance, byte>>();

        public class InstantBattleTask
        {
            public static void Run(Tuple<MapInstance, byte> mapinstance)
            {
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                Task.Delay(10 * 1000);
                if (!mapinstance.Item1.Sessions.Skip(1 - 1).Any())
                {
                    mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList().ForEach(s =>
                    {
                        s.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>()
                        {
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide),
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide),
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Ambush)
                        });
                        ServerManager.Instance.ChangeMap(s.Character.CharacterId, s.Character.MapId, s.Character.MapX, s.Character.MapY);
                    });
                }
                Observable.Timer(TimeSpan.FromMinutes(12)).Subscribe(async X =>
                {
                    for (int d = 0; d < 180; d++)
                    {
                        if (!mapinstance.Item1.Monsters.Any(s => s.CurrentHp > 0))
                        {
                            EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SPAWNPORTAL, new Portal { SourceX = 47, SourceY = 33, DestinationMapId = 1 }));
                            mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("INSTANTBATTLE_SUCCEEDED"), 0));
                            foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                            {
                                await HandleRewards(cli);
                            }
                            break;
                        }
                        await Task.Delay(1000);
                    }
                });

                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(15), new EventContainer(mapinstance.Item1, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(3), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 12), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 10), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 5), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 4), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 3), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(13), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 2), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(14), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"), 1), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(14.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(14.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("INSTANTBATTLE_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, "msg 4 A horde of monsters is slowly approaching..."));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, "msg 4 Woah! Monsters have appeared!"));

                for (int wave = 0; wave < 4; wave++)
                {
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(130 + (wave * 160)), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, "msg 4 The monsters will appear in 40 seconds"));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(160 + (wave * 160)), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, "msg 4 A horde of monsters is slowly approaching..."));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(170 + (wave * 160)), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, "msg 4 Woah! Monsters have appeared!"));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10 + (wave * 160)), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getInstantBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave)));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(140 + (wave * 160)), new EventContainer(mapinstance.Item1, EventActionType.DROPITEMS, getInstantBattleDrop(mapinstance.Item1.Map, mapinstance.Item2, wave)));
                }
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(650), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getInstantBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, 4)));
            }

            private static IEnumerable<Tuple<short, int, short, short>> generateDrop(Map map, short vnum, int amountofdrop, int amount)
            {
                List<Tuple<short, int, short, short>> dropParameters = new List<Tuple<short, int, short, short>>();
                for (int i = 0; i < amountofdrop; i++)
                {
                    MapCell cell = map.GetRandomPosition();
                    dropParameters.Add(new Tuple<short, int, short, short>(vnum, amount, cell.X, cell.Y));
                }
                return dropParameters;
            }

            public static async Task HandleRewards(ClientSession cli)
            {
                cli.Character.Reputation += InstantBattleConfiguration.ReputationEarned(cli.Character.Level);
                MessageExtension.SendGrey(cli, $"You have earned {InstantBattleConfiguration.ReputationEarned(cli.Character.Level)} Reputation as a Reward");

                cli.Character.Gold += InstantBattleConfiguration.GoldEarned(cli.Character.Level);
                cli.SendPacket(cli.Character.GenerateGold());
                MessageExtension.SendGrey(cli, $"You have earned {InstantBattleConfiguration.GoldEarned(cli.Character.Level)} Gold as a Reward");

                if (cli.Character.Family != null)
                {
                    cli.Character.GenerateFamilyXp(InstantBattleConfiguration.FamilyXPEarned(cli.Character.Level));
                    MessageExtension.SendGrey(cli, $"You have earned {InstantBattleConfiguration.FamilyXPEarned(cli.Character.Level)} Family XP as a Reward");
                }
            }

            private static List<Tuple<short, int, short, short>> getInstantBattleDrop(Map map, short instantbattletype, int wave)
            {
                List<Tuple<short, int, short, short>> dropParameters = new List<Tuple<short, int, short, short>>();
                switch (instantbattletype)
                {
                    case 80:
                        switch (wave)
                        {
                            case 0:
                                dropParameters.AddRange(generateDrop(map, 1046, 15, 10000));
                                dropParameters.AddRange(generateDrop(map, 1011, 15, 5));
                                dropParameters.AddRange(generateDrop(map, 1246, 15, 1));
                                break;

                            case 1:
                                dropParameters.AddRange(generateDrop(map, 1046, 15, 12000));
                                dropParameters.AddRange(generateDrop(map, 1011, 15, 5));
                                dropParameters.AddRange(generateDrop(map, 1247, 15, 1));
                                break;

                            case 2:
                                dropParameters.AddRange(generateDrop(map, 1046, 15, 15000));
                                dropParameters.AddRange(generateDrop(map, 1011, 20, 5));
                                dropParameters.AddRange(generateDrop(map, 1246, 15, 1));
                                dropParameters.AddRange(generateDrop(map, 1247, 15, 1));
                                break;

                            case 3:
                                dropParameters.AddRange(generateDrop(map, 1046, 30, 20000));
                                dropParameters.AddRange(generateDrop(map, 1011, 30, 5));
                                dropParameters.AddRange(generateDrop(map, 1030, 30, 1));
                                dropParameters.AddRange(generateDrop(map, 2282, 12, 3));
                                break;
                        }
                        break;
                }
                return dropParameters;
            }

            private static ConcurrentBag<MonsterToSummon> getInstantBattleMonster(Map map, short instantbattletype, int wave)
            {
                ConcurrentBag<MonsterToSummon> summonParameters = new ConcurrentBag<MonsterToSummon>();

                switch (instantbattletype)
                {
                    case 80:
                        switch (wave)
                        {
                            case 0:
                                map.GenerateMonsters(1007, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1003, 15, false, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1002, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1001, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1000, 16, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                break;

                            case 1:
                                map.GenerateMonsters(1199, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1198, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1197, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1196, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1123, 16, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                break;

                            case 2:
                                map.GenerateMonsters(1305, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1304, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1303, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1302, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1194, 16, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                break;

                            case 3:
                                map.GenerateMonsters(1902, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1901, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1900, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1045, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1043, 15, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1042, 16, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                break;

                            case 4:
                                map.GenerateMonsters(637, 1, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1903, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1053, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1051, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1049, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1048, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                map.GenerateMonsters(1047, 13, true, new List<EventContainer>()).ToList().ForEach(s => summonParameters.Add(s));
                                break;
                        }
                        break;
                }
                return summonParameters;
            }
        }
    }
}
