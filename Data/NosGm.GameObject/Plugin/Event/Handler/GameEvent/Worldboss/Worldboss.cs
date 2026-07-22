using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace NosGm.GameObject.Event
{
    public static class WorldBoss
    {
        private const short BossVnum = 994;
        private const short MapVnum = 2004;
        private static readonly TimeSpan BossTime = new(0, 10, 0);

        public static void GenerateWorldBoss()
        {
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event will start in 5 Minutes");
            ServerManager.Instance.Broadcast("msg 1 The Worldboss Event will start in 5 Minutes");
            Thread.Sleep(4 * 60 * 1000);
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event will start in 1 Minute");
            ServerManager.Instance.Broadcast("msg 1 The Worldboss Event will start in 1 Minute");
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event will start in 30 Seconds");
            ServerManager.Instance.Broadcast("msg 1 The Worldboss Event will start in 30 Seconds");
            Thread.Sleep(20 * 1000);
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event will start in 10 Seconds");
            ServerManager.Instance.Broadcast("msg 1 The Worldboss Event will start in 10 Seconds");
            Thread.Sleep(10 * 1000);
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event started");
            ServerManager.Instance.Sessions.Where(s => s.Character?.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList().ForEach(s => s.SendPacket($"qnaml 100 #guri^506 Do you want to join the fight?"));
            ServerManager.Instance.EventInWaiting = true;
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Broadcast("msg 0 The Worldboss Event started");
            ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == false).ToList().ForEach(s => s.SendPacket("esf"));
            ServerManager.Instance.EventInWaiting = false;

            var sessions = ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);
            var map = ServerManager.GenerateMapInstance(MapVnum, MapInstanceType.NormalInstance, new InstanceBag());

            foreach (var s in sessions)
            {
                ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                s.Character.IsWaitingForEvent = false;
            }
            ServerManager.Instance.StartedEvents.Remove(EventType.WORLDBOSS);
            WorldBossTask.Run(map);
        }

        public class WorldBossTask
        {
            public static void Run(MapInstance mapinstance)
            {
                Thread.Sleep(10 * 1000); //10 Seconds Delay Cool

                #region Remove Invis
                if (!mapinstance.Sessions.Skip(1 - 1).Any())
                {
                    mapinstance.Sessions.Where(s => s.Character != null).ToList().ForEach(s =>
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
                #endregion

                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes), new EventContainer(mapinstance, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 10), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_MINUTES_REMAINING"), 10), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 5), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_MINUTES_REMAINING"), 5), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 3), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_MINUTES_REMAINING"), 3), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 2), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_MINUTES_REMAINING"), 2), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 1), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_MINUTES_REMAINING"), 1), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(BossTime.TotalMinutes - 0.5), new EventContainer(mapinstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WORLDBOSS_SECONDS_REMAINING"), 30), 0)));

                var cancellationToken = new CancellationTokenSource();
                EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNMONSTERS, mapinstance.Map.GenerateMonsters(BossVnum, 1, true, new List<EventContainer>(), false, true, true)));

                mapinstance.Sessions.Where(s => s.Character != null).ToList().ForEach(s =>
                {
                    s.SendPacket("bsinfo 1 18 1200 10");
                });

                Observable.Interval(TimeSpan.FromSeconds(1)).Timeout(BossTime).Subscribe(_ =>
                {
                    if (!mapinstance.Monsters.Any(s => s.CurrentHp > 0))
                    {
                        EventHelper.Instance.RunEvent(new EventContainer(mapinstance, EventActionType.SPAWNPORTAL, new Portal { SourceX = 39, SourceY = 12, DestinationMapId = 1 }));
                        mapinstance.Broadcast("msg 3 The Worldboss has been defeated!");
                        foreach (var cli in mapinstance.Sessions.Where(s => s.Character != null).ToList())
                        {
                            cli.Character.GiftAdd(2172, 1);
                            cli.Character.GiftAdd(9287, 5);
                            cli.Character.GiftAdd(2333, 10);
                            cli.Character.GiftAdd(1363, 5);
                            cli.Character.GiftAdd(1364, 5);
                            cli.Character.GiftAdd(5369, 5);
                            cli.Character.GiftAdd(5815, 5);
                            cli.Character.GiftAdd(9574, 5);
                            InstanceExtension.AddBattlePassPoint(cli);
                            cli.SendPacket("bsinfo 2");
                            cancellationToken.Cancel();
                        }
                    }
                }, () => { EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance, EventActionType.DISPOSEMAP, null)); }, cancellationToken.Token);
            }
        }
    }
}