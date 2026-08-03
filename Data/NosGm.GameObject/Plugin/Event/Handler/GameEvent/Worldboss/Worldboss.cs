using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject.Event
{
    public static class WorldBoss
    {
        private const short BossVnum = 994;
        private const short MapVnum = 2004;
        private static readonly TimeSpan BossTime = TimeSpan.FromMinutes(10);

        public static void GenerateWorldBoss()
        {
            Task.Run(RunEventAsync);
        }

        private static async Task RunEventAsync()
        {
            MapInstance eventMap = null;

            try
            {
                BroadcastCountdown("5 Minutes");
                await Task.Delay(TimeSpan.FromMinutes(4)).ConfigureAwait(false);
                BroadcastCountdown("1 Minute");
                await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                BroadcastCountdown("30 Seconds");
                await Task.Delay(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                BroadcastCountdown("10 Seconds");
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                ServerManager.Instance.Broadcast("msg 0 The Worldboss Event started");
                List<ClientSession> eligibleSessions = ServerManager.Instance.Sessions
                    .Where(session =>
                        session?.Character?.MapInstance != null &&
                        session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    .ToList();

                foreach (ClientSession session in eligibleSessions)
                {
                    session.SendPacket("qnaml 100 #guri^506 Do you want to join the fight?");
                }

                ServerManager.Instance.EventInWaiting = true;
                await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                ServerManager.Instance.EventInWaiting = false;

                foreach (ClientSession session in ServerManager.Instance.Sessions
                             .Where(current => current?.Character?.IsWaitingForEvent == false)
                             .ToList())
                {
                    session.SendPacket("esf");
                }

                List<ClientSession> participants = ServerManager.Instance.Sessions
                    .Where(session =>
                        session?.Character?.IsWaitingForEvent == true &&
                        session.Character.MapInstance != null &&
                        session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    .ToList();

                eventMap = ServerManager.GenerateMapInstance(
                    MapVnum,
                    MapInstanceType.NormalInstance,
                    new InstanceBag());
                if (eventMap == null)
                {
                    throw new InvalidOperationException($"World Boss map {MapVnum} could not be created.");
                }

                foreach (ClientSession participant in participants)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(
                        participant,
                        eventMap.MapInstanceId);
                    participant.Character.IsWaitingForEvent = false;
                }

                Logger.Info(
                    $"[WORLD_BOSS] Result=LobbyClosed Participants={participants.Count} " +
                    $"MapInstance={eventMap.MapInstanceId}");
                await WorldBossRuntime.RunAsync(eventMap).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.Error("[WORLD_BOSS] Result=Failed", exception);
                if (eventMap != null)
                {
                    DisposeMap(eventMap);
                }
            }
            finally
            {
                ServerManager.Instance.EventInWaiting = false;
                foreach (ClientSession session in ServerManager.Instance.Sessions
                             .Where(current => current?.Character != null)
                             .ToList())
                {
                    session.Character.IsWaitingForEvent = false;
                }

                Plugin.Event.GameEventHandler.CompleteEvent(EventType.WORLDBOSS);
            }
        }

        private static void BroadcastCountdown(string remaining)
        {
            ServerManager.Instance.Broadcast(
                $"msg 0 The Worldboss Event will start in {remaining}");
            ServerManager.Instance.Broadcast(
                $"msg 1 The Worldboss Event will start in {remaining}");
        }

        private static void DisposeMap(MapInstance map)
        {
            EventHelper.Instance.RunEvent(
                new EventContainer(map, EventActionType.DISPOSEMAP, null));
        }

        private static class WorldBossRuntime
        {
            public static async Task RunAsync(MapInstance mapInstance)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                RemoveInvisibility(mapInstance);
                ScheduleWarnings(mapInstance);

                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        mapInstance,
                        EventActionType.SPAWNMONSTERS,
                        mapInstance.Map.GenerateMonsters(
                            BossVnum,
                            1,
                            true,
                            new List<EventContainer>(),
                            false,
                            true,
                            true)));

                foreach (ClientSession session in mapInstance.Sessions
                             .Where(current => current?.Character != null)
                             .ToList())
                {
                    session.SendPacket("bsinfo 1 18 1200 10");
                }

                DateTime deadline = DateTime.UtcNow.Add(BossTime);
                while (DateTime.UtcNow < deadline)
                {
                    if (!mapInstance.Monsters.Any(monster => monster?.CurrentHp > 0))
                    {
                        await CompleteVictoryAsync(mapInstance).ConfigureAwait(false);
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }

                mapInstance.Broadcast("msg 3 The Worldboss escaped because time ran out.");
                foreach (ClientSession session in mapInstance.Sessions
                             .Where(current => current?.Character != null)
                             .ToList())
                {
                    session.SendPacket("bsinfo 2");
                }

                Logger.Warn(
                    $"[WORLD_BOSS] Result=TimedOut MapInstance={mapInstance.MapInstanceId}");
                DisposeMap(mapInstance);
            }

            private static async Task CompleteVictoryAsync(MapInstance mapInstance)
            {
                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        mapInstance,
                        EventActionType.SPAWNPORTAL,
                        new Portal
                        {
                            SourceX = 39,
                            SourceY = 12,
                            DestinationMapId = 1
                        }));
                mapInstance.Broadcast("msg 3 The Worldboss has been defeated!");

                List<ClientSession> winners = mapInstance.Sessions
                    .Where(session => session?.Character != null)
                    .ToList();
                foreach (ClientSession winner in winners)
                {
                    winner.Character.GiftAdd(2172, 1);
                    winner.Character.GiftAdd(9287, 5);
                    winner.Character.GiftAdd(2333, 10);
                    winner.Character.GiftAdd(1363, 5);
                    winner.Character.GiftAdd(1364, 5);
                    winner.Character.GiftAdd(5369, 5);
                    winner.Character.GiftAdd(5815, 5);
                    winner.Character.GiftAdd(9574, 5);
                    InstanceExtension.AddBattlePassPoint(winner);
                    winner.SendPacket("bsinfo 2");
                }

                Logger.Info(
                    $"[WORLD_BOSS] Result=Defeated MapInstance={mapInstance.MapInstanceId} " +
                    $"Rewarded={winners.Count}");
                await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                DisposeMap(mapInstance);
            }

            private static void RemoveInvisibility(MapInstance mapInstance)
            {
                foreach (ClientSession session in mapInstance.Sessions
                             .Where(current => current?.Character != null)
                             .ToList())
                {
                    session.Character.RemoveBuffByBCardTypeSubType(
                        new List<KeyValuePair<byte, byte>>
                        {
                            new KeyValuePair<byte, byte>(
                                (byte)BCardType.CardType.SpecialActions,
                                (byte)AdditionalTypes.SpecialActions.Hide),
                            new KeyValuePair<byte, byte>(
                                (byte)BCardType.CardType.FalconSkill,
                                (byte)AdditionalTypes.FalconSkill.Hide),
                            new KeyValuePair<byte, byte>(
                                (byte)BCardType.CardType.FalconSkill,
                                (byte)AdditionalTypes.FalconSkill.Ambush)
                        });
                }
            }

            private static void ScheduleWarnings(MapInstance mapInstance)
            {
                ScheduleWarning(mapInstance, TimeSpan.Zero, "WORLDBOSS_MINUTES_REMAINING", 10);
                ScheduleWarning(mapInstance, TimeSpan.FromMinutes(5), "WORLDBOSS_MINUTES_REMAINING", 5);
                ScheduleWarning(mapInstance, TimeSpan.FromMinutes(7), "WORLDBOSS_MINUTES_REMAINING", 3);
                ScheduleWarning(mapInstance, TimeSpan.FromMinutes(8), "WORLDBOSS_MINUTES_REMAINING", 2);
                ScheduleWarning(mapInstance, TimeSpan.FromMinutes(9), "WORLDBOSS_MINUTES_REMAINING", 1);
                ScheduleWarning(mapInstance, TimeSpan.FromMinutes(9.5), "WORLDBOSS_SECONDS_REMAINING", 30);
            }

            private static void ScheduleWarning(
                MapInstance mapInstance,
                TimeSpan delay,
                string messageKey,
                int value)
            {
                EventHelper.Instance.ScheduleEvent(
                    delay,
                    new EventContainer(
                        mapInstance,
                        EventActionType.SENDPACKET,
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(
                                Language.Instance.GetMessageFromKey(messageKey),
                                value),
                            0)));
            }
        }
    }
}
