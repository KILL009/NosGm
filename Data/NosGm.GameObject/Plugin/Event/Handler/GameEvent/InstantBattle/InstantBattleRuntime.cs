using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Configuration.GameEvent;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Domain;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;

namespace NosGm.GameObject.Plugin.Event
{
    public static class InstantBattleRuntime
    {
        private const int MaximumPlayersPerInstance = 50;
        private static readonly TimeSpan BattleDuration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CompletionCheckStart = TimeSpan.FromMinutes(12);

        public static void GenerateInstantBattle()
        {
            RunLobbyAsync().GetAwaiter().GetResult();
        }

        private static async Task RunLobbyAsync()
        {
            await BroadcastThenWaitAsync(
                "Instant Battle will start in 5 Minute(s)",
                TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            await BroadcastThenWaitAsync(
                "Instant Battle will start in 1 Minute(s)",
                TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            await BroadcastThenWaitAsync(
                "Instant Battle will start in 30 Seconds",
                TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            await BroadcastThenWaitAsync(
                "Instant Battle will start in 10 Seconds",
                TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            await BroadcastThenWaitAsync(
                "Instant Battle has begun",
                TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            ServerManager.Instance.EventInWaiting = true;
            foreach (ClientSession session in ServerManager.Instance.Sessions
                         .Where(candidate =>
                             candidate?.Character?.MapInstance != null &&
                             candidate.Character.MapInstance.MapInstanceType ==
                             MapInstanceType.BaseMapInstance)
                         .ToList())
            {
                session.SendPacket("qnaml 1 #guri^506 Do you want to join the Battle?");
            }

            await BroadcastThenWaitAsync(
                "Instant Battle started",
                TimeSpan.FromSeconds(30)).ConfigureAwait(false);

            foreach (ClientSession session in ServerManager.Instance.Sessions
                         .Where(candidate =>
                             candidate?.Character != null &&
                             !candidate.Character.IsWaitingForEvent)
                         .ToList())
            {
                session.SendPacket("esf");
            }

            ServerManager.Instance.EventInWaiting = false;
            var waitingSessions = ServerManager.Instance.Sessions
                .Where(candidate =>
                    candidate?.Character?.MapInstance != null &&
                    candidate.Character.IsWaitingForEvent &&
                    candidate.Character.MapInstance.MapInstanceType ==
                    MapInstanceType.BaseMapInstance)
                .ToList();

            var brackets = new Dictionary<byte, byte>
            {
                { 1, 39 },
                { 40, 49 },
                { 50, 59 },
                { 60, 69 },
                { 70, 79 },
                { 80, 99 }
            };

            var instances = new List<Tuple<MapInstance, byte>>();
            foreach (KeyValuePair<byte, byte> bracket in brackets)
            {
                List<ClientSession> bracketSessions = waitingSessions
                    .Where(session =>
                        session.Character.Level >= bracket.Key &&
                        session.Character.Level <= bracket.Value &&
                        !session.Character.IsMuted())
                    .ToList();
                CreateInstances(bracketSessions, bracket.Key, instances);
            }

            foreach (ClientSession session in ServerManager.Instance.Sessions
                         .Where(candidate => candidate?.Character != null)
                         .ToList())
            {
                session.Character.IsWaitingForEvent = false;
            }

            GameEventHandler.CompleteEvent(EventType.INSTANTBATTLE);
            Logger.Info(
                $"[INSTANT_BATTLE] Result=LobbyClosed " +
                $"Participants={waitingSessions.Count} Instances={instances.Count}");

            foreach (Tuple<MapInstance, byte> instance in instances)
            {
                _ = RunInstanceAsync(instance).ContinueWith(
                    task => Logger.Error(
                        task.Exception?.GetBaseException(),
                        $"[INSTANT_BATTLE] Result=InstanceFailed " +
                        $"Instance={instance.Item1?.MapInstanceId} Bracket={instance.Item2}"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
        }

        private static async Task BroadcastThenWaitAsync(string message, TimeSpan delay)
        {
            ServerManager.Instance.Broadcast("msg 6 " + message, ReceiverType.All);
            await Task.Delay(delay).ConfigureAwait(false);
        }

        private static void CreateInstances(
            IList<ClientSession> sessions,
            byte levelBracket,
            ICollection<Tuple<MapInstance, byte>> destination)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return;
            }

            List<ClientSession> shuffledSessions = sessions.ToList().Shuffle();
            MapInstance currentMap = null;

            for (int index = 0; index < shuffledSessions.Count; index++)
            {
                ClientSession session = shuffledSessions[index];
                if (session?.Character == null)
                {
                    continue;
                }

                if (currentMap == null || index % MaximumPlayersPerInstance == 0)
                {
                    currentMap = ServerManager.GenerateMapInstance(
                        2004,
                        MapInstanceType.NormalInstance,
                        new InstanceBag());
                    if (currentMap == null)
                    {
                        throw new InvalidOperationException(
                            $"Instant Battle map 2004 could not be generated for bracket {levelBracket}.");
                    }

                    destination.Add(new Tuple<MapInstance, byte>(currentMap, levelBracket));
                }

                ServerManager.Instance.TeleportOnRandomPlaceInMap(
                    session,
                    currentMap.MapInstanceId);
            }
        }

        private static async Task RunInstanceAsync(Tuple<MapInstance, byte> instance)
        {
            MapInstance map = instance?.Item1;
            if (map == null || map.Map == null)
            {
                throw new InvalidOperationException("Instant Battle received an invalid map instance.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            if (!map.Sessions.Any(session => session?.Character != null))
            {
                SafeRun(
                    map,
                    "DisposeEmptyInstance",
                    () => EventHelper.Instance.RunEvent(
                        new EventContainer(map, EventActionType.DISPOSEMAP, null)));
                return;
            }

            Logger.Info(
                $"[INSTANT_BATTLE] Result=InstanceStarted " +
                $"Instance={map.MapInstanceId} Bracket={instance.Item2} " +
                $"Players={map.Sessions.Count(session => session?.Character != null)}");

            SchedulePacket(map, TimeSpan.Zero, "msg 4 A horde of monsters is slowly approaching...");
            SchedulePacket(map, TimeSpan.FromSeconds(10), "msg 4 Woah! Monsters have appeared!");

            SchedulePacket(
                map,
                TimeSpan.FromMinutes(3),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        12),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(5),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        10),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(10),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        5),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(11),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        4),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(12),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        3),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(13),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        2),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(14),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_MINUTES_REMAINING"),
                        1),
                    0));
            SchedulePacket(
                map,
                TimeSpan.FromMinutes(14.5),
                UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("INSTANTBATTLE_SECONDS_REMAINING"),
                        30),
                    0));

            for (int wave = 0; wave < 4; wave++)
            {
                int capturedWave = wave;
                SchedulePacket(
                    map,
                    TimeSpan.FromSeconds(130 + capturedWave * 160),
                    "msg 4 The monsters will appear in 40 seconds");
                SchedulePacket(
                    map,
                    TimeSpan.FromSeconds(160 + capturedWave * 160),
                    "msg 4 A horde of monsters is slowly approaching...");
                SchedulePacket(
                    map,
                    TimeSpan.FromSeconds(170 + capturedWave * 160),
                    "msg 4 Woah! Monsters have appeared!");

                Schedule(
                    map,
                    TimeSpan.FromSeconds(10 + capturedWave * 160),
                    $"SpawnWave:{capturedWave}",
                    () =>
                    {
                        List<MonsterToSummon> monsters =
                            InstantBattleWaveCatalog.GetMonsters(
                                map.Map,
                                instance.Item2,
                                capturedWave);
                        if (monsters.Count == 0)
                        {
                            Logger.Warn(
                                $"[INSTANT_BATTLE] Result=EmptyWave " +
                                $"Instance={map.MapInstanceId} Bracket={instance.Item2} " +
                                $"Wave={capturedWave}");
                            return;
                        }

                        map.SummonMonsters(monsters);
                        Logger.Info(
                            $"[INSTANT_BATTLE] Result=WaveSpawned " +
                            $"Instance={map.MapInstanceId} Bracket={instance.Item2} " +
                            $"Wave={capturedWave} Monsters={monsters.Count}");
                    });

                Schedule(
                    map,
                    TimeSpan.FromSeconds(140 + capturedWave * 160),
                    $"DropWave:{capturedWave}",
                    () => map.DropItems(
                        InstantBattleWaveCatalog.GetDrops(
                            map.Map,
                            instance.Item2,
                            capturedWave)));
            }

            Schedule(
                map,
                TimeSpan.FromSeconds(650),
                "SpawnFinalWave",
                () =>
                {
                    List<MonsterToSummon> monsters =
                        InstantBattleWaveCatalog.GetMonsters(map.Map, instance.Item2, 4);
                    map.SummonMonsters(monsters);
                    Logger.Info(
                        $"[INSTANT_BATTLE] Result=FinalWaveSpawned " +
                        $"Instance={map.MapInstanceId} Bracket={instance.Item2} " +
                        $"Monsters={monsters.Count}");
                });

            _ = MonitorCompletionAsync(map);
            Schedule(
                map,
                BattleDuration,
                "DisposeInstance",
                () => EventHelper.Instance.RunEvent(
                    new EventContainer(map, EventActionType.DISPOSEMAP, null)));
        }

        private static async Task MonitorCompletionAsync(MapInstance map)
        {
            await Task.Delay(CompletionCheckStart).ConfigureAwait(false);

            for (int elapsedSeconds = 0; elapsedSeconds < 180; elapsedSeconds++)
            {
                if (!map.Monsters.Any(monster => monster != null && monster.IsAlive && monster.CurrentHp > 0))
                {
                    SafeRun(
                        map,
                        "CompleteInstance",
                        () =>
                        {
                            EventHelper.Instance.RunEvent(
                                new EventContainer(
                                    map,
                                    EventActionType.SPAWNPORTAL,
                                    new Portal
                                    {
                                        SourceX = 47,
                                        SourceY = 33,
                                        DestinationMapId = 1
                                    }));
                            map.Broadcast(
                                UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("INSTANTBATTLE_SUCCEEDED"),
                                    0));

                            foreach (ClientSession session in map.Sessions
                                         .Where(candidate => candidate?.Character != null)
                                         .ToList())
                            {
                                HandleRewards(session);
                            }

                            Logger.Info(
                                $"[INSTANT_BATTLE] Result=Succeeded " +
                                $"Instance={map.MapInstanceId}");
                        });
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }

        private static void HandleRewards(ClientSession session)
        {
            int reputation = InstantBattleConfiguration.ReputationEarned(session.Character.Level);
            long goldReward = InstantBattleConfiguration.GoldEarned(session.Character.Level);
            int familyXp = InstantBattleConfiguration.FamilyXPEarned(session.Character.Level);

            session.Character.Reputation += reputation;
            MessageExtension.SendGrey(
                session,
                $"You have earned {reputation} Reputation as a Reward");

            long maximumGold = ServerManager.Instance.Configuration.MaxGold;
            session.Character.Gold = Math.Min(
                maximumGold,
                session.Character.Gold + goldReward);
            session.SendPacket(session.Character.GenerateGold());
            MessageExtension.SendGrey(
                session,
                $"You have earned {goldReward} Gold as a Reward");

            if (session.Character.Family != null)
            {
                session.Character.GenerateFamilyXp(familyXp);
                MessageExtension.SendGrey(
                    session,
                    $"You have earned {familyXp} Family XP as a Reward");
            }
        }

        private static void SchedulePacket(
            MapInstance map,
            TimeSpan delay,
            string packet)
        {
            Schedule(
                map,
                delay,
                "SendPacket",
                () => EventHelper.Instance.RunEvent(
                    new EventContainer(map, EventActionType.SENDPACKET, packet)));
        }

        private static void Schedule(
            MapInstance map,
            TimeSpan delay,
            string operation,
            Action action)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay).ConfigureAwait(false);
                SafeRun(map, operation, action);
            });
        }

        private static void SafeRun(
            MapInstance map,
            string operation,
            Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    exception,
                    $"[INSTANT_BATTLE] Result=ActionFailed " +
                    $"Operation={operation} Instance={map?.MapInstanceId}");
            }
        }
    }
}
