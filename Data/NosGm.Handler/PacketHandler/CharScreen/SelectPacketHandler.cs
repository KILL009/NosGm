using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Handler.Services;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Packets.Packets.ClientPackets;

namespace NosGm.Handler.Packets.CharScreenPackets
{
    public class SelectPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession Session;

        #endregion

        #region Instantiation

        public SelectPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Methods

        public void SelectCharacter(SelectPacket selectPacket)
        {
            try
            {
                #region Validate Session

                if (Session?.Account == null
                    || Session.HasSelectedCharacter)
                {
                    return;
                }

                #endregion

                #region Load Character

                CharacterDTO characterDTO = DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, selectPacket.Slot);

                if (characterDTO == null)
                {
                    return;
                }

                Character character = new Character(characterDTO);

                #endregion

                #region Unban Character

                if (ServerManager.Instance.BannedCharacters.Contains(character.CharacterId))
                {
                    ServerManager.Instance.BannedCharacters.RemoveAll(s => s == character.CharacterId);
                }

                #endregion

                #region Initialize Character

                character.Initialize();

                short MapId = character.MapId;
                short MapX = character.MapX;
                short MapY = character.MapY;
                if (ServerManager.Instance.ChannelId == 51)
                {
                    if (character.Faction == FactionType.Angel)
                        MapId = 130;
                    else
                        MapId = 131;

                    MapX = 40;
                    MapY = 42;
                }

                character.MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(MapId);
                character.PositionX = MapX;
                character.PositionY = MapY;
                character.Authority = Session.Account.Authority;

                Session.SetCharacter(character);

                #endregion

                #region General Logs

                // GeneralLog is an append-only audit table. Loading the complete account
                // history here caused large EF materialization and GC spikes during login.
                character.GeneralLogs = new ThreadSafeGenericList<GeneralLogDTO>();

                #endregion

                #region Limitations

                // If there are > 3 accounts connected, kick this session.
                if (CommunicationServiceClient.Instance.RetrieveOnlineCharacters(character.CharacterId).Count() > 3)
                {
                    Session.Disconnect();
                }

                #endregion

                #region FishingLogs

                DAOFactory.CharacterFishDAO.LoadByCharacterId(Session.Character.CharacterId)?.ToList().ForEach(s => Session.Character.FishingLogs.Add(s));

                #endregion

                #region Other Character Stuff

                Session.Character.Respawns = DAOFactory.RespawnDAO.LoadByCharacter(Session.Character.CharacterId).ToList();
                Session.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(Session.Character.CharacterId).ToList();
                Session.Character.LoadInventory();
                Session.Character.LoadQuicklists();
                Session.Character.GenerateMiniland();

                #endregion

                #region Quests

                if (!DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.IsMainQuest) && !DAOFactory.QuestLogDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.QuestId == 1997))
                {
                    CharacterQuestDTO firstQuest = new CharacterQuestDTO
                    {
                        CharacterId = Session.Character.CharacterId,
                        //QuestId = 1997,
                        //IsMainQuest = false
                    };

                    DAOFactory.CharacterQuestDAO.InsertOrUpdate(firstQuest);
                }

                DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));

                //DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList()
                //    .ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));

                #endregion

                #region Fix Partner Slots

                if (character.MaxPartnerCount < 3)
                {
                    character.MaxPartnerCount = 3;
                }

                #endregion

                #region Load Mates

                DAOFactory.MateDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                {
                    Mate mate = new Mate(s)
                    {
                        Owner = Session.Character
                    };

                    mate.GenerateMateTransportId();
                    mate.Monster = ServerManager.GetNpcMonster(s.NpcMonsterVNum);

                    Session.Character.Mates.Add(mate);
                });

                #endregion

                #region Load Permanent Buff

                Session.Character.LastPermBuffRefresh = DateTime.Now;

                #endregion

                #region CharacterLife

                // One dedicated scheduler now services every connected character.
                // This removes one Observable.Interval and one recurring callback per player.
                Session.Character.Life = null;
                CharacterLifeScheduler.EnsureStarted();

                #endregion

                #region Title

                DAOFactory.CharacterTitleDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                {
                    Session.Character.Title.Add(s);
                });

                #endregion

                #region Battle Pass
                //DAOFactory.CharacterBattlePassDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                //{
                //    Session.Character.CharacterBattlePass.Add(s);
                //});
                #endregion

                #region Load Amulet

                Observable.Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(o =>
                    {
                        ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);

                        if (amulet?.ItemDeleteTime != null || amulet?.DurabilityPoint > 0)
                        {
                            Session.Character.AddBuff(new Buff(62, Session.Character.Level), Session.Character.BattleEntity);
                        }
                    });

                #endregion

                #region Load Static Buff

                foreach (StaticBuffDTO staticBuff in DAOFactory.StaticBuffDAO.LoadByCharacterId(Session.Character.CharacterId))
                {
                    if (staticBuff.CardId != 319 /* Wedding */)
                    {
                        Session.Character.AddStaticBuff(staticBuff);
                    }
                }

                #endregion

                #region RemoveAllDupedShell

                int cleanedShells = DAOFactory.ShellEffectDAO.CleanupDuplicateNonRuneEffects(
                    Session.Character.CharacterId);
                if (cleanedShells > 0)
                {
                    Logger.Warn($"Removed duplicate shell effects from {cleanedShells} equipment serials for CharacterId {Session.Character.CharacterId}.");
                }

                #endregion

                #region Enter the World

                Session.SendPacket("OK");

                CommunicationServiceClient.Instance.ConnectCharacter(
                    ServerManager.Instance.WorldId,
                    Session.Account.AccountId,
                    Session.SessionId,
                    character.CharacterId);

                character.Channel = ServerManager.Instance;

                #endregion
            }
            catch (Exception ex)
            {
                Logger.Error("Failed selecting the character.", ex);
            }
            finally
            {
                // Suspicious activity detected -- kick!
                if (Session != null && (!Session.HasSelectedCharacter || Session.Character == null))
                {
                    Session.Disconnect();
                }
            }
        }

        #endregion
    }
}

namespace NosGm.Handler.Services
{
    public sealed class CharacterLifeSchedulerSnapshot
    {
        public bool IsRunning { get; internal set; }
        public int IntervalMilliseconds { get; internal set; }
        public int ActiveCharacters { get; internal set; }
        public long Ticks { get; internal set; }
        public long CharacterExecutions { get; internal set; }
        public long SkippedSessions { get; internal set; }
        public long Errors { get; internal set; }
        public long Overruns { get; internal set; }
        public long MissedTicks { get; internal set; }
        public double AverageTickMilliseconds { get; internal set; }
        public double MaximumTickMilliseconds { get; internal set; }
        public double AverageCharacterMilliseconds { get; internal set; }
        public double MaximumCharacterMilliseconds { get; internal set; }
        public double AverageLagMilliseconds { get; internal set; }
        public double MaximumLagMilliseconds { get; internal set; }
        public DateTime? LastTickUtc { get; internal set; }
    }

    internal sealed class CharacterLifeSchedulerCounters
    {
        public long Ticks;
        public long CharacterExecutions;
        public long SkippedSessions;
        public long Errors;
        public long Overruns;
        public long MissedTicks;
        public long TotalTickTicks;
        public long MaximumTickTicks;
        public long TotalCharacterTicks;
        public long MaximumCharacterTicks;
        public long TotalLagTicks;
        public long MaximumLagTicks;
    }

    public static class CharacterLifeScheduler
    {
        public const int IntervalMilliseconds = 300;

        private static readonly object StartSync = new object();
        private static readonly ManualResetEventSlim StopSignal = new ManualResetEventSlim(false);

        private static CharacterLifeSchedulerCounters _counters = new CharacterLifeSchedulerCounters();
        private static Thread _worker;
        private static int _started;
        private static int _activeCharacters;
        private static long _lastTickUtcTicks;
        private static long _lastErrorLogUtcTicks;

        public static void EnsureStarted()
        {
            if (Volatile.Read(ref _started) != 0)
            {
                return;
            }

            lock (StartSync)
            {
                if (_started != 0)
                {
                    return;
                }

                _worker = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "NosGM-CharacterLife-Scheduler"
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, args) => Stop();
                Volatile.Write(ref _started, 1);
                _worker.Start();
            }
        }

        public static CharacterLifeSchedulerSnapshot Capture()
        {
            CharacterLifeSchedulerCounters counters = Volatile.Read(ref _counters);
            long ticks = Interlocked.Read(ref counters.Ticks);
            long characterExecutions = Interlocked.Read(ref counters.CharacterExecutions);
            long lastTickTicks = Interlocked.Read(ref _lastTickUtcTicks);

            return new CharacterLifeSchedulerSnapshot
            {
                IsRunning = _worker?.IsAlive == true && !StopSignal.IsSet,
                IntervalMilliseconds = IntervalMilliseconds,
                ActiveCharacters = Volatile.Read(ref _activeCharacters),
                Ticks = ticks,
                CharacterExecutions = characterExecutions,
                SkippedSessions = Interlocked.Read(ref counters.SkippedSessions),
                Errors = Interlocked.Read(ref counters.Errors),
                Overruns = Interlocked.Read(ref counters.Overruns),
                MissedTicks = Interlocked.Read(ref counters.MissedTicks),
                AverageTickMilliseconds = StopwatchTicksToMilliseconds(
                    ticks == 0 ? 0 : Interlocked.Read(ref counters.TotalTickTicks) / ticks),
                MaximumTickMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref counters.MaximumTickTicks)),
                AverageCharacterMilliseconds = StopwatchTicksToMilliseconds(
                    characterExecutions == 0
                        ? 0
                        : Interlocked.Read(ref counters.TotalCharacterTicks) / characterExecutions),
                MaximumCharacterMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref counters.MaximumCharacterTicks)),
                AverageLagMilliseconds = StopwatchTicksToMilliseconds(
                    ticks == 0 ? 0 : Interlocked.Read(ref counters.TotalLagTicks) / ticks),
                MaximumLagMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref counters.MaximumLagTicks)),
                LastTickUtc = lastTickTicks > 0
                    ? new DateTime(lastTickTicks, DateTimeKind.Utc)
                    : (DateTime?)null
            };
        }

        public static void ResetMetrics()
        {
            Interlocked.Exchange(ref _counters, new CharacterLifeSchedulerCounters());
        }

        private static void Run()
        {
            long intervalTicks = Math.Max(
                1,
                (long)(Stopwatch.Frequency * (IntervalMilliseconds / 1000d)));
            long nextTick = Stopwatch.GetTimestamp() + intervalTicks;

            while (!StopSignal.IsSet)
            {
                WaitUntil(nextTick);
                if (StopSignal.IsSet)
                {
                    return;
                }

                long tickStarted = Stopwatch.GetTimestamp();
                long lagTicks = Math.Max(0, tickStarted - nextTick);
                CharacterLifeSchedulerCounters counters = Volatile.Read(ref _counters);

                ExecuteTick(counters);

                long tickFinished = Stopwatch.GetTimestamp();
                long elapsedTicks = Math.Max(0, tickFinished - tickStarted);
                Interlocked.Increment(ref counters.Ticks);
                Interlocked.Add(ref counters.TotalTickTicks, elapsedTicks);
                AtomicMaximum(ref counters.MaximumTickTicks, elapsedTicks);
                Interlocked.Add(ref counters.TotalLagTicks, lagTicks);
                AtomicMaximum(ref counters.MaximumLagTicks, lagTicks);
                Interlocked.Exchange(ref _lastTickUtcTicks, DateTime.UtcNow.Ticks);

                nextTick += intervalTicks;
                if (tickFinished >= nextTick)
                {
                    long missed = ((tickFinished - nextTick) / intervalTicks) + 1;
                    Interlocked.Increment(ref counters.Overruns);
                    Interlocked.Add(ref counters.MissedTicks, missed);
                    nextTick += missed * intervalTicks;
                }
            }
        }

        private static void ExecuteTick(CharacterLifeSchedulerCounters counters)
        {
            List<ClientSession> sessions;
            try
            {
                sessions = ServerManager.Instance.Sessions?.ToList() ?? new List<ClientSession>();
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref counters.Errors);
                LogSchedulerError("Unable to snapshot World sessions for CharacterLife.", exception);
                return;
            }

            int activeCharacters = 0;
            foreach (ClientSession session in sessions)
            {
                if (session == null || !session.HasSelectedCharacter ||
                    !session.IsConnected || session.IsDisposing)
                {
                    Interlocked.Increment(ref counters.SkippedSessions);
                    continue;
                }

                Character character;
                try
                {
                    character = session.Character;
                }
                catch (Exception exception)
                {
                    Interlocked.Increment(ref counters.Errors);
                    LogSchedulerError("Unable to resolve a session character for CharacterLife.", exception);
                    continue;
                }

                if (character == null || character.IsDisposed)
                {
                    Interlocked.Increment(ref counters.SkippedSessions);
                    continue;
                }

                activeCharacters++;
                long characterStarted = Stopwatch.GetTimestamp();
                try
                {
                    character.CharacterLife();
                    Interlocked.Increment(ref counters.CharacterExecutions);
                }
                catch (Exception exception)
                {
                    Interlocked.Increment(ref counters.Errors);
                    LogSchedulerError(
                        $"CharacterLife failed for CharacterId {character.CharacterId} ({character.Name}).",
                        exception);
                }
                finally
                {
                    long elapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - characterStarted);
                    Interlocked.Add(ref counters.TotalCharacterTicks, elapsedTicks);
                    AtomicMaximum(ref counters.MaximumCharacterTicks, elapsedTicks);
                }
            }

            Volatile.Write(ref _activeCharacters, activeCharacters);
        }

        private static void WaitUntil(long targetTimestamp)
        {
            while (!StopSignal.IsSet)
            {
                long remainingTicks = targetTimestamp - Stopwatch.GetTimestamp();
                if (remainingTicks <= 0)
                {
                    return;
                }

                int waitMilliseconds = (int)Math.Min(
                    100,
                    Math.Max(1, remainingTicks * 1000L / Stopwatch.Frequency));
                StopSignal.Wait(waitMilliseconds);
            }
        }

        private static void Stop()
        {
            if (Volatile.Read(ref _started) == 0)
            {
                return;
            }

            StopSignal.Set();
            Thread worker = _worker;
            if (worker != null && worker != Thread.CurrentThread)
            {
                worker.Join(TimeSpan.FromSeconds(3));
            }
        }

        private static void LogSchedulerError(string message, Exception exception)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long previousTicks = Interlocked.Read(ref _lastErrorLogUtcTicks);
            if (nowTicks - previousTicks < TimeSpan.FromSeconds(30).Ticks)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref _lastErrorLogUtcTicks,
                    nowTicks,
                    previousTicks) == previousTicks)
            {
                Logger.Error(message, exception);
            }
        }

        private static void AtomicMaximum(ref long target, long value)
        {
            long current = Interlocked.Read(ref target);
            while (value > current)
            {
                long observed = Interlocked.CompareExchange(ref target, value, current);
                if (observed == current)
                {
                    return;
                }
                current = observed;
            }
        }

        private static double StopwatchTicksToMilliseconds(long ticks) =>
            ticks <= 0 ? 0 : ticks * 1000d / Stopwatch.Frequency;
    }
}
