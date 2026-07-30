using NosGm.Domain;
using System;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Library.Client
{
    public static class CommunicationGlobalEventMapper
    {
        public static EventType ToDomain(
            WireV1.CommunicationGlobalEventType eventType)
        {
            switch (eventType)
            {
                case WireV1.CommunicationGlobalEventType.InstantBattle:
                    return EventType.INSTANTBATTLE;
                case WireV1.CommunicationGlobalEventType.LandOfDeath:
                    return EventType.LOD;
                case WireV1.CommunicationGlobalEventType.MinilandRefresh:
                    return EventType.MINILANDREFRESHEVENT;
                case WireV1.CommunicationGlobalEventType.RankingRefresh:
                    return EventType.RANKINGREFRESH;
                case WireV1.CommunicationGlobalEventType.GlacernonShip:
                    return EventType.GLACERNONSHIP;
                case WireV1.CommunicationGlobalEventType.GlacernonRaid:
                    return EventType.GLACERNONRAID;
                case WireV1.CommunicationGlobalEventType.MeteoriteGame:
                    return EventType.METEORITEGAME;
                case WireV1.CommunicationGlobalEventType.TalentArena:
                    return EventType.TALENTARENA;
                case WireV1.CommunicationGlobalEventType.Caligor:
                    return EventType.CALIGOR;
                case WireV1.CommunicationGlobalEventType.IceBreaker:
                    return EventType.ICEBREAKER;
                case WireV1.CommunicationGlobalEventType.AutoReboot:
                    return EventType.AUTOREBOOT;
                case WireV1.CommunicationGlobalEventType.Act7Ship:
                    return EventType.Act7Ship;
                case WireV1.CommunicationGlobalEventType.CelestialSpire:
                    return EventType.CELESTIALSPIRE;
                case WireV1.CommunicationGlobalEventType.RainbowBattle:
                    return EventType.RAINBOWBATTLE;
                case WireV1.CommunicationGlobalEventType.DropRate:
                    return EventType.DROPRATE;
                case WireV1.CommunicationGlobalEventType.FairyRate:
                    return EventType.FAIRYRATE;
                case WireV1.CommunicationGlobalEventType.HeroRate:
                    return EventType.HERORATE;
                case WireV1.CommunicationGlobalEventType.XpRate:
                    return EventType.XPRATE;
                case WireV1.CommunicationGlobalEventType.ResetRate:
                    return EventType.RESETRATE;
                case WireV1.CommunicationGlobalEventType.DailyMissionExtensionRefresh:
                    return EventType.DAILYMISSIONEXTENSIONREFRESH;
                case WireV1.CommunicationGlobalEventType.Asgobas:
                    return EventType.ASGOBAS;
                case WireV1.CommunicationGlobalEventType.WorldBoss:
                    return EventType.WORLDBOSS;
                case WireV1.CommunicationGlobalEventType.BattleRoyale:
                    return EventType.BattleRoyal;
                case WireV1.CommunicationGlobalEventType.DuelEvent:
                    return EventType.DUELEVENT;
                case WireV1.CommunicationGlobalEventType.PrivateDuelEvent:
                    return EventType.DUELEVENTPRIVATE;
                case WireV1.CommunicationGlobalEventType.OpenWorldBoss:
                    return EventType.OpenWorldBoss;
                default:
                    throw new InvalidOperationException(
                        "The communication global-event type is unsupported.");
            }
        }

        public static WireV1.CommunicationGlobalEventType ToWire(
            EventType eventType)
        {
            switch (eventType)
            {
                case EventType.INSTANTBATTLE:
                    return WireV1.CommunicationGlobalEventType.InstantBattle;
                case EventType.LOD:
                    return WireV1.CommunicationGlobalEventType.LandOfDeath;
                case EventType.MINILANDREFRESHEVENT:
                    return WireV1.CommunicationGlobalEventType.MinilandRefresh;
                case EventType.RANKINGREFRESH:
                    return WireV1.CommunicationGlobalEventType.RankingRefresh;
                case EventType.GLACERNONSHIP:
                    return WireV1.CommunicationGlobalEventType.GlacernonShip;
                case EventType.GLACERNONRAID:
                    return WireV1.CommunicationGlobalEventType.GlacernonRaid;
                case EventType.METEORITEGAME:
                    return WireV1.CommunicationGlobalEventType.MeteoriteGame;
                case EventType.TALENTARENA:
                    return WireV1.CommunicationGlobalEventType.TalentArena;
                case EventType.CALIGOR:
                    return WireV1.CommunicationGlobalEventType.Caligor;
                case EventType.ICEBREAKER:
                    return WireV1.CommunicationGlobalEventType.IceBreaker;
                case EventType.AUTOREBOOT:
                    return WireV1.CommunicationGlobalEventType.AutoReboot;
                case EventType.Act7Ship:
                    return WireV1.CommunicationGlobalEventType.Act7Ship;
                case EventType.CELESTIALSPIRE:
                    return WireV1.CommunicationGlobalEventType.CelestialSpire;
                case EventType.RAINBOWBATTLE:
                    return WireV1.CommunicationGlobalEventType.RainbowBattle;
                case EventType.DROPRATE:
                    return WireV1.CommunicationGlobalEventType.DropRate;
                case EventType.FAIRYRATE:
                    return WireV1.CommunicationGlobalEventType.FairyRate;
                case EventType.HERORATE:
                    return WireV1.CommunicationGlobalEventType.HeroRate;
                case EventType.XPRATE:
                    return WireV1.CommunicationGlobalEventType.XpRate;
                case EventType.RESETRATE:
                    return WireV1.CommunicationGlobalEventType.ResetRate;
                case EventType.DAILYMISSIONEXTENSIONREFRESH:
                    return WireV1.CommunicationGlobalEventType.DailyMissionExtensionRefresh;
                case EventType.ASGOBAS:
                    return WireV1.CommunicationGlobalEventType.Asgobas;
                case EventType.WORLDBOSS:
                    return WireV1.CommunicationGlobalEventType.WorldBoss;
                case EventType.BattleRoyal:
                    return WireV1.CommunicationGlobalEventType.BattleRoyale;
                case EventType.DUELEVENT:
                    return WireV1.CommunicationGlobalEventType.DuelEvent;
                case EventType.DUELEVENTPRIVATE:
                    return WireV1.CommunicationGlobalEventType.PrivateDuelEvent;
                case EventType.OpenWorldBoss:
                    return WireV1.CommunicationGlobalEventType.OpenWorldBoss;
                default:
                    throw new InvalidOperationException(
                        "The domain global-event type is unsupported.");
            }
        }
    }
}
