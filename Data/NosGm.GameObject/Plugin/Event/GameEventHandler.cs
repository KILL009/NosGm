using System.Collections;
using System.Threading.Tasks;
using NosGm.Domain;
using NosGm.GameObject.Event;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event.Handler;
using NosGm.Packets.Packets.CommandPackets;
using Game.Configuration;
using NosGm.GameObject.TitanShield;
using NosGm.GameObject.TitanShield.Thread;
using NosGm.GameObject.Event.ARENA;
using System;

namespace NosGm.GameObject.Plugin.Event
{
    public static class GameEventHandler
    {
        public static void GenerateEvent(EventType type, int LvlBracket = -1, byte value = 0)
        {
            try
            {
                if (type == EventType.ICEBREAKER && LvlBracket < 0)
                {
                    return;
                }
                if (!ServerManager.Instance.StartedEvents.Contains(type))
                {
                    Task.Run(() =>
                    {
                        ServerManager.Instance.StartedEvents.Add(type);
                        switch (type)
                        {

                            case EventType.RANKINGREFRESH:
                                ServerManager.Instance.RefreshRanking();
                                ServerManager.Instance.StartedEvents?.Remove(type);
                                break;

                            case EventType.LOD:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    LandOfDeath.GenerateLandOfDeath();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;


                            case EventType.MINILANDREFRESHEVENT:
                                MinilandRefresh.GenerateMinilandEvent();
                                break;

                            case EventType.DAILYMISSIONEXTENSIONREFRESH:
                                ServerManager.Instance.RefreshDailyMissions();
                                ServerManager.Instance.StartedEvents.Remove(type);
                                break;

                            case EventType.INSTANTBATTLE:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    InstantBattle.GenerateInstantBattle();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.RAINBOWBATTLE:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    RainbowBattle.GenerateEvent();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.GLACERNONSHIP:
                                GlacernonShip.GenerateGlacernonShip(1);
                                GlacernonShip.GenerateGlacernonShip(2);
                                break;

                            case EventType.TALENTARENA:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    ArenaEvent.GenerateTalentArena();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.CALIGOR:
                                if (ServerManager.Instance.ChannelId == 51)
                                {
                                    CaligorRaid.Run();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.ICEBREAKER:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    IceBreaker.GenerateIceBreaker(LvlBracket);
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.WORLDBOSS:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    WorldBoss.GenerateWorldBoss();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.ASGOBAS:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    AsgobasInstantBattle.GenerateInstantBattle();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents?.Remove(type);
                                }
                                break;

                            case EventType.AUTOREBOOT:
                                ServerManager.Instance.IsReboot = true;
                                ServerManager.Instance.RebootTask = new Task(ServerManager.Instance.AutoReboot);
                                ServerManager.Instance.RebootTask.Start();
                                break;

                            default:
                                ServerManager.Instance.StartedEvents?.Remove(type);
                                break;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogServer.Logger.LogAsync($"Something went wrong with the Events. Error: {Environment.NewLine}" + ex, LogType.ERROR);
            }

        }
    }
}
