using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace NosGm.GameObject.Plugin.Event
{
    public class RainbowBattleManager
    {
        #region Methods

        public static void AddFlag(ClientSession ses, RainbowBattleTeam rbb, RainbowNpcType type, int npcId)
        {
            if (rbb == null)
            {
                return;
            }

            rbb.Score += (byte)type;

            rbb.TotalFlag.Add(new Tuple<int, RainbowNpcType>(npcId, type));

            SendIcoFlagOnMinimap(ses, npcId, (byte)type, (byte)(rbb.TeamEntity == RainbowTeamBattleType.Blue ? 2 : 1));

            var RainbowTeam2 = rbb.SecondTeam;

            if (RainbowTeam2 == null)
            {
                return;
            }

            if (AlreadyHaveFlag(RainbowTeam2, type, npcId))
            {
                RemoveFlag(RainbowTeam2, type, npcId);
            }

            SendFbs(ses.CurrentMapInstance);
            ses.CurrentMapInstance.Broadcast($"msg 0 {rbb.TeamEntity} Team captured the {type} Crystal and Scored {(byte)type} points [Score: {rbb.Score} - {rbb.SecondTeam.Score}]");
        }

        public static bool AlreadyHaveFlag(RainbowBattleTeam RainbowBattleTeam, RainbowNpcType type, int NpcId)
        {
            if (RainbowBattleTeam == null)
            {
                return false;
            }

            var a = RainbowBattleTeam.TotalFlag.FindAll(s => s.Item1 == NpcId && s.Item2 == type).Count();

            return a == 0 ? false : true;
        }

        public static bool AreNotInMap(ClientSession ses)
        {
            if (ses == null)
            {
                return false;
            }

            if (ses?.CurrentMapInstance?.MapInstanceType != MapInstanceType.RainbowBattleInstance)
            {
                return true;
            }
            return false;
        }

        public static void EndEvent(ClientSession ses, MapInstance map)
        {
            if (ses == null)
            {
                return;
            }

            map.IsPVP = false;
            var rbb = ServerManager.Instance.RainbowBattleMembers.Find(s => s.Session.Contains(ses));

            if (rbb == null || rbb.SecondTeam == null)
            {
                return;
            }

            bool teamWinner = (rbb.Score > rbb.SecondTeam.Score ? true : false);

            if (rbb.Score == rbb.SecondTeam.Score)
            {
                SendItem(rbb.Session, 2);
                SendItem(rbb.SecondTeam.Session, 2);
            }
            else
            {
                SendItem(rbb.Session, teamWinner ? (byte)1 : (byte)0);
                SendItem(rbb.SecondTeam.Session, !teamWinner ? (byte)1 : (byte)0);
            }
            
            ServerManager.Instance.RainbowBattleMembers.Remove(rbb);
            ServerManager.Instance.RainbowBattleMembers.Remove(rbb.SecondTeam);
        }

        public static void GenerateScore(RainbowBattleTeam RainbowBattle)
        {
            if (RainbowBattle == null)
            {
                return;
            }

            var first = GetFlag(RainbowBattle, RainbowNpcType.Small);
            var Second = GetFlag(RainbowBattle, RainbowNpcType.Second);
            var Last = GetFlag(RainbowBattle, RainbowNpcType.Large);

            var total = first + (Second * 2) + (Last * 3);

            RainbowBattle.Score += total;

            foreach (var ses in RainbowBattle.Session)
            {
                SendFbs(ses.CurrentMapInstance);
            }
        }

        public static void GenerateScoreForAll()
        {
            foreach (var rbb in ServerManager.Instance.RainbowBattleMembers)
            {
                GenerateScore(rbb);
            }
        }

        public static void ReceiveFbs(RainbowBattleTeam rbb)
        {
            if (rbb == null)
            {
                return;
            }

            var output = (rbb.TeamEntity == RainbowTeamBattleType.Red ? rbb.SecondTeam : rbb);

            foreach (var bb in rbb.Session)
            {
                if (bb == null && AreNotInMap(bb)) continue;

                bb?.SendPacket(
                    $"fbs " +
                    $"{(byte)rbb.TeamEntity} " +
                    $"{rbb.Session.Count()} " +
                    $"{output.SecondTeam.Score} " +
                    $"{output.Score} " +
                    $"{GetFlag(rbb, RainbowNpcType.Small)} " +
                    $"{GetFlag(rbb, RainbowNpcType.Second)} " +
                    $"{GetFlag(rbb, RainbowNpcType.Large)} " +
                    $"{rbb.TeamEntity}");
            }
        }

        public static void RemoveFlag(RainbowBattleTeam RainbowBattle, RainbowNpcType type, int NpcId)
        {
            if (RainbowBattle == null)
            {
                return;
            }

            RainbowBattle.TotalFlag.RemoveAll(s => s.Item1 == NpcId && s.Item2 == type);
        }

        public static void SendFbs(MapInstance map)
        {
            if (map == null)
            {
                return;
            }

            foreach (var ses in map.Sessions)
            {
                if (AreNotInMap(ses))
                {
                    continue;
                }

                var rbb = ServerManager.Instance.RainbowBattleMembers.Find(s => s.Session.Contains(ses));

                if (rbb == null)
                {
                    continue;
                }

                ReceiveFbs(rbb);
            }
        }

        public static void SendIcoFlagOnMinimap(ClientSession sess, long npcId, byte score, byte team)
        {
            sess.CurrentMapInstance?.Broadcast($"fbt 6 {npcId} {score} {team}");
        }

        private static int GetFlag(RainbowBattleTeam RainbowBattleTeam, RainbowNpcType type)
        {
            if (RainbowBattleTeam == null)
            {
                return 0;
            }
            return RainbowBattleTeam.TotalFlag.FindAll(s => s.Item2 == type).Count();
        }

        private static void SendItem(IEnumerable<ClientSession> sess, byte winner)
        {
            foreach (var ses in sess)
            {
                if (sess == null)
                {
                    continue;
                }

                if (AreNotInMap(ses))
                {
                    continue;
                }

                ses.Character.Group?.LeaveGroup(ses);
                ServerManager.Instance.UpdateGroup(ses.Character.CharacterId);
                ses.SendPacket(ses.Character.GenerateRaid(2, true));

                Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                {
                    ServerManager.Instance.ChangeMap(ses.Character.CharacterId, ses.Character.MapId, ses.Character.MapX, ses.Character.MapY);
                });

                // Loose
                if (winner == 0)
                {
                    int goldrewardlose = 5000000;
                    ses.Character.GiftAdd(5746, 1);
                    ses.Character.GiftAdd(2361, 1);
                    ses.Character.GetReputation(ses.Character.Level * 50);
                    ses.SendPacket(ses.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), ses.Character.Level * 50), 10));
                    ses.Character.RBBLose++;
                    ses.Character.RbbDead = 0;
                    ses.Character.RbbKill = 0;
                    ses.Character.MandraCount = 0;
                    if (ses.Character.Gold + goldrewardlose < GameConfiguration.MaxGold)
                    {
                        ses.Character.Gold += goldrewardlose;
                    }
                    else
                    {
                        ses.SendPacket(ses.Character.GenerateSay($"You can't get more gold.", 10));
                        return;
                    }
                    ses.Character.GenerateGold();
                    ses?.SendPacket(ses?.Character.GenerateSay($"You lost this Rainbow Battle, don't give up!", 10));
                }

                // Win
                if (winner == 1)
                {
                    int goldrewardwin = 10000000;
                    ses.Character.GiftAdd(5746, 2);
                    ses.Character.GiftAdd(2361, 3);
                    ses.Character.GiftAdd(7338, 1);
                    ses.Character.GiftAdd(7481, 1);
                    ses.Character.GetReputation(ses.Character.Level * 200);
                    ses.SendPacket(ses.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), ses.Character.Level * 200), 10));
                    
                    ses.Character.RBBWin++;
                    ses.Character.RbbDead = 0;
                    ses.Character.RbbKill = 0;
                    ses.Character.MandraCount = 0;
                    if (ses.Character.Level < 99)
                    {
                        ses.Character.Level++;
                        ses.Character.GenerateStat();
                        ses.Character.GenerateLevelUp();
                    }
                    if (ses.Character.HeroLevel < 30)
                    {
                        ses.Character.HeroLevel++;
                        ses.Character.GenerateStat();
                        ses.Character.GenerateLevelUp();
                    }
                    
                    if (ses.Character.Gold + goldrewardwin < GameConfiguration.MaxGold)
                    {
                        ses.Character.Gold += goldrewardwin;
                    }
                    else
                    {
                        ses?.SendPacket(ses?.Character.GenerateSay($"You can't get more gold...", 10));
                        return;
                    }                   
                    ses.Character.GenerateGold();
                    ses.SendPacket(ses.Character.GenerateSay($"You won this Rainbow Battle, you get 10.000.000 Gold!", 10));
                }

                // Equal
                if (winner == 2)
                {
                    int drawgold = 2500000;
                    ses.Character.GiftAdd(5746, 1);
                    ses.Character.GiftAdd(2361, 2);
                    ses.Character.GetReputation(ses.Character.Level * 100);
                    ses.SendPacket(ses.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), ses.Character.Level * 100), 10));
                    ses.Character.RbbDead = 0;
                    ses.Character.RbbKill = 0;
                    ses.Character.MandraCount = 0;
                   
                    if (ses.Character.Gold + drawgold < GameConfiguration.MaxGold)
                    {
                        ses.Character.Gold += drawgold;
                    }
                    else
                    {
                        ses.SendPacket(ses.Character.GenerateSay($"You can't get more gold.", 10));
                        return;
                    }                   
                    ses.Character.GenerateGold();
                    ses.SendPacket(ses.Character.GenerateSay($"Wow!, Nobody Won or Lost... Next time Crew!.", 10));
                }
            }
        }

        #endregion
    }
}