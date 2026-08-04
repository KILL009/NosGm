using System;
using System.Threading.Tasks;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Event.ARENA;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event.Handler;
using NosGm.GameObject.TitanShield;
using NosGm.GameObject.TitanShield.Thread;

namespace NosGm.GameObject.Plugin.Event
{
    public static class GameEventHandler
    {
        private const int MaximumRuntimeRate = 1000;
        private const int DefaultXpRate = 5;
        private const int DefaultHeroXpRate = 5;
        private const int DefaultDropRate = 1;
        private const int DefaultFairyXpRate = 10;

        private static readonly object EventStateSync = new object();

        public static void GenerateEvent(EventType type, int LvlBracket = -1, byte value = 0)
        {
            if (type == EventType.ICEBREAKER && LvlBracket < 0)
            {
                Logger.Warn($"[EVENT_RUNTIME] Event={type} Result=Rejected Reason=MissingLevelBracket");
                return;
            }

            lock (EventStateSync)
            {
                if (ServerManager.Instance.StartedEvents.Contains(type))
                {
                    Logger.Warn($"[EVENT_RUNTIME] Event={type} Result=Ignored Reason=AlreadyStarted");
                    return;
                }

                ServerManager.Instance.StartedEvents.Add(type);
            }

            Task.Run(() =>
            {
                try
                {
                    Logger.Info(
                        $"[EVENT_RUNTIME] Event={type} Result=Starting " +
                        $"Channel={ServerManager.Instance.ChannelId} LevelBracket={LvlBracket} Value={value}");
                    DispatchEvent(type, LvlBracket, value);
                }
                catch (Exception exception)
                {
                    CompleteEvent(type);
                    Logger.Error(
                        $"[EVENT_RUNTIME] Event={type} Result=Failed " +
                        $"Channel={ServerManager.Instance.ChannelId} LevelBracket={LvlBracket} Value={value}",
                        exception);
                }
            });
        }

        public static void CompleteEvent(EventType type)
        {
            lock (EventStateSync)
            {
                ServerManager.Instance.StartedEvents.Remove(type);
            }
        }

        private static void DispatchEvent(EventType type, int levelBracket, byte value)
        {
            switch (type)
            {
                case EventType.RANKINGREFRESH:
                    ServerManager.Instance.RefreshRanking();
                    CompleteEvent(type);
                    break;
                case EventType.LOD:
                    if (ServerManager.Instance.ChannelId != 51) LandOfDeath.GenerateLandOfDeath();
                    else CompleteEvent(type);
                    break;
                case EventType.MINILANDREFRESHEVENT:
                    MinilandRefresh.GenerateMinilandEvent();
                    break;
                case EventType.DAILYMISSIONEXTENSIONREFRESH:
                    ServerManager.Instance.RefreshDailyMissions();
                    CompleteEvent(type);
                    break;
                case EventType.INSTANTBATTLE:
                    if (ServerManager.Instance.ChannelId != 51) InstantBattleRuntime.GenerateInstantBattle();
                    else CompleteEvent(type);
                    break;
                case EventType.RAINBOWBATTLE:
                    if (ServerManager.Instance.ChannelId != 51) RainbowBattle.GenerateEvent();
                    else CompleteEvent(type);
                    break;
                case EventType.GLACERNONSHIP:
                    GlacernonShip.GenerateGlacernonShip(1);
                    GlacernonShip.GenerateGlacernonShip(2);
                    break;
                case EventType.TALENTARENA:
                    if (ServerManager.Instance.ChannelId != 51) ArenaEvent.GenerateTalentArena();
                    else CompleteEvent(type);
                    break;
                case EventType.CALIGOR:
                    if (ServerManager.Instance.ChannelId == 51) CaligorRaid.Run();
                    else CompleteEvent(type);
                    break;
                case EventType.ICEBREAKER:
                    if (ServerManager.Instance.ChannelId != 51) IceBreaker.GenerateIceBreaker(levelBracket);
                    else CompleteEvent(type);
                    break;
                case EventType.WORLDBOSS:
                    if (ServerManager.Instance.ChannelId != 51) WorldBoss.GenerateWorldBoss();
                    else CompleteEvent(type);
                    break;
                case EventType.ASGOBAS:
                    if (ServerManager.Instance.ChannelId != 51) AsgobasInstantBattle.GenerateInstantBattle();
                    else CompleteEvent(type);
                    break;
                case EventType.AUTOREBOOT:
                    ServerManager.Instance.IsReboot = true;
                    ServerManager.Instance.RebootTask = new Task(ServerManager.Instance.AutoReboot);
                    ServerManager.Instance.RebootTask.Start();
                    break;
                case EventType.DROPRATE:
                case EventType.FAIRYRATE:
                case EventType.HERORATE:
                case EventType.XPRATE:
                    ApplyRateEvent(type, ResolveRuntimeRate(levelBracket, value));
                    CompleteEvent(type);
                    break;
                case EventType.RESETRATE:
                    ResetRuntimeRates();
                    CompleteEvent(type);
                    break;
                case EventType.GLACERNONRAID:
                case EventType.METEORITEGAME:
                case EventType.Act7Ship:
                case EventType.CELESTIALSPIRE:
                case EventType.BattleRoyal:
                case EventType.DUELEVENT:
                case EventType.DUELEVENTPRIVATE:
                case EventType.OpenWorldBoss:
                    Logger.Warn($"[EVENT_RUNTIME] Event={type} Result=UnsupportedLocalDispatch Channel={ServerManager.Instance.ChannelId} Value={value}");
                    CompleteEvent(type);
                    break;
                default:
                    Logger.Warn($"[EVENT_RUNTIME] Event={type} Result=UnknownEvent Channel={ServerManager.Instance.ChannelId}");
                    CompleteEvent(type);
                    break;
            }
        }

        private static int ResolveRuntimeRate(int levelBracket, byte value)
        {
            int rate = value > 0 ? value : levelBracket;
            if (rate <= 0 || rate > MaximumRuntimeRate)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelBracket),
                    rate,
                    $"Runtime rates must be between 1 and {MaximumRuntimeRate}.");
            }

            return rate;
        }

        private static void ApplyRateEvent(EventType type, int rate)
        {
            switch (type)
            {
                case EventType.DROPRATE:
                    GameConfiguration.DropRate = rate;
                    break;
                case EventType.FAIRYRATE:
                    GameConfiguration.FairyXPRate = rate;
                    break;
                case EventType.HERORATE:
                    GameConfiguration.HeroXPRate = rate;
                    break;
                case EventType.XPRATE:
                    GameConfiguration.XPRate = rate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Not a rate event.");
            }

            Logger.Info($"[EVENT_RUNTIME] Event={type} Result=RateApplied Rate={rate}");
        }

        private static void ResetRuntimeRates()
        {
            GameConfiguration.XPRate = DefaultXpRate;
            GameConfiguration.HeroXPRate = DefaultHeroXpRate;
            GameConfiguration.DropRate = DefaultDropRate;
            GameConfiguration.FairyXPRate = DefaultFairyXpRate;

            Logger.Info(
                $"[EVENT_RUNTIME] Event={EventType.RESETRATE} Result=RatesReset " +
                $"XP={DefaultXpRate} HeroXP={DefaultHeroXpRate} " +
                $"Drop={DefaultDropRate} FairyXP={DefaultFairyXpRate}");
        }
    }
}
