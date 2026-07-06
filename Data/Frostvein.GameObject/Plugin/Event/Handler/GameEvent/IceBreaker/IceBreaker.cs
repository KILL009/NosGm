using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Frostvein.GameObject.Plugin.Event
{
    public class IceBreaker
    {
        #region Members

        public const int MaxAllowedPlayers = 50;

        public static List<ClientSession> FrozenPlayers = new List<ClientSession>();

        public static List<ConcurrentBag<ClientSession>> IceBreakerTeams = new List<ConcurrentBag<ClientSession>>();

        private static readonly int[] GoldRewards =
                        {
            40000,
            80000,
            120000,
            160000,
            200000,
            240000
        };

        private static readonly Tuple<int, int>[] LevelBrackets =
        {
            new Tuple<int, int>(1, 25),
            new Tuple<int, int>(20, 40),
            new Tuple<int, int>(35, 55),
            new Tuple<int, int>(50, 70),
            new Tuple<int, int>(65, 85),
            new Tuple<int, int>(80, 99)
        };

        private static int currentBracket;

        #endregion

        #region Properties

        public static List<ClientSession> AlreadyFrozenPlayers { get; set; }

        public static MapInstance Map { get; private set; }

        #endregion

        #region Methods

        public static void GenerateIceBreaker(int Bracket)
        {
            if (Bracket < 0)
            {
                return;
            }
            currentBracket = Bracket;
            AlreadyFrozenPlayers = new List<ClientSession>();
            Map = ServerManager.GenerateMapInstance(2005, MapInstanceType.IceBreakerInstance, new InstanceBag());

            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(
                   string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_MINUTES"), 5, LevelBrackets[currentBracket].Item1, LevelBrackets[currentBracket].Item2), 1));
            Thread.Sleep(5 * 60 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(
                string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_MINUTES"), 1, LevelBrackets[currentBracket].Item1, LevelBrackets[currentBracket].Item2), 1));
            Thread.Sleep(1 * 60 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(
                string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_SECONDS"), 30, LevelBrackets[currentBracket].Item1, LevelBrackets[currentBracket].Item2), 1));
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(
                string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_SECONDS"), 10, LevelBrackets[currentBracket].Item1, LevelBrackets[currentBracket].Item2), 1));
            Thread.Sleep(10 * 1000);

            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_STARTED"), 1));
            ServerManager.Instance.IceBreakerInWaiting = true;
            ServerManager.Instance.Sessions.Where(x => x.Character.Level >= LevelBrackets[currentBracket].Item1 && x.Character.Level <= LevelBrackets[currentBracket].Item2 && x.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList().ForEach(x => x.SendPacket($"qnaml 2 #guri^501 {string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_ASK"), 500)}"));
            /*currentBracket++;
            if (currentBracket > 5)
            {
                currentBracket = 0;
            }*/

            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(c =>
            {
                ServerManager.Instance.StartedEvents.Remove(EventType.ICEBREAKER);
                ServerManager.Instance.IceBreakerInWaiting = false;
                if (Map.Sessions.Count() <= 1)
                {
                    Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_WIN"), 0));
                    Map.Sessions.ToList().ForEach(x =>
                    {
                        x.Character.GetReputation(x.Character.Level * 100);
                        InstanceExtension.AddBattlePassPoint(x);
                        if (x.Character.Dignity < 100)
                        {
                            x.Character.Dignity = 100;
                        }

                        x.Character.Gold += GoldRewards[currentBracket];
                        x.Character.Gold = x.Character.Gold > GameConfiguration.MaxGold ? GameConfiguration.MaxGold : x.Character.Gold;
                        x.SendPacket(x.Character.GenerateFd());
                        x.CurrentMapInstance?.Broadcast(x, x.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                        x.CurrentMapInstance?.Broadcast(x, x.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        x.SendPacket(x.Character.GenerateGold());
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_MONEY"), GoldRewards[currentBracket]), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), x.Character.Level * 100), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_RESTORED"), 100), 10));
                    });
                    Thread.Sleep(5000);
                    IceBreakerTeams.Clear();
                    AlreadyFrozenPlayers.Clear();
                    FrozenPlayers.Clear();
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(Map, EventActionType.DISPOSEMAP, null));
                }
                else
                {
                    Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN"), 0));
                    Thread.Sleep(6000);
                    Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN_AGAIN"), 0));
                    Thread.Sleep(7000);
                    Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN_AGAIN_WARN"), 0));
                    Thread.Sleep(1000);
                    Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_START"), 0));
                    Map.IsPVP = true;

                    ConcurrentBag<ClientSession> WinnerTeam = null;
                    for (int d = 0; d < 1800; d++)
                    {
                        foreach (ClientSession cli in Map.Sessions.Where(s => !FrozenPlayers.Contains(s)).ToList())
                        {
                            if (WinnerTeam == null)
                            {
                                WinnerTeam = IceBreakerTeams.FirstOrDefault(t => t.Contains(cli));
                            }
                            else if (!WinnerTeam.Contains(cli))
                            {
                                WinnerTeam = null;
                                break;
                            }
                        }
                        if (Map.Sessions.Count(s => !FrozenPlayers.Contains(s)) == 1)
                        {
                            WinnerTeam = new ConcurrentBag<ClientSession>();
                            WinnerTeam.Add(Map.Sessions.FirstOrDefault(s => !FrozenPlayers.Contains(s)));
                        }
                        if (WinnerTeam != null)
                        {
                            WinnerTeam.ToList().ForEach(x =>
                            {
                                x.Character.GetReputation(x.Character.Level * 200);
                                if (x.Character.Dignity < 100)
                                {
                                    x.Character.Dignity = 100;
                                }


                                x.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_WIN"), 0));
                                x.Character.Gold += GoldRewards[currentBracket];
                                x.Character.Gold = x.Character.Gold > GameConfiguration.MaxGold ? GameConfiguration.MaxGold : x.Character.Gold;
                                x.SendPacket(x.Character.GenerateFd());
                                x.CurrentMapInstance?.Broadcast(x, x.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                                x.CurrentMapInstance?.Broadcast(x, x.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                                x.SendPacket(x.Character.GenerateGold());
                                x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_MONEY"), GoldRewards[currentBracket]), 10));
                                x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), x.Character.Level * 200), 10));
                                x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_RESTORED"), x.Character.Level * 10), 10));
                            });
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(Map, EventActionType.DISPOSEMAP, null));
                    Observable.Timer(TimeSpan.FromSeconds(20)).Subscribe(X =>
                    {
                        IceBreakerTeams.Clear();
                        AlreadyFrozenPlayers.Clear();
                        FrozenPlayers.Clear();
                    });
                }
            });
        }

        #endregion
    }
}