using Frostvein.Packets.Packets.ClientPackets;

using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.HttpClients;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.GameObject.Helpers
{
    public class UserInterfaceHelper
    {
        #region Members

        private static UserInterfaceHelper _instance;

        #endregion

        #region Properties

        public static UserInterfaceHelper Instance => _instance ?? (_instance = new UserInterfaceHelper());

        #endregion

        #region Methods

        public static string GenerateBazarRecollect(long pricePerUnit, int soldAmount, int amount, long taxes, long totalPrice, string name) => $"rc_scalc 1 {pricePerUnit} {soldAmount} {amount} {taxes} {totalPrice} {name.Replace(' ', '^')}";

        public static string GenerateBSInfo(byte mode, short title, short time, short text) => $"bsinfo {mode} {title} {time} {text}";

        public static string GenerateCHDM(int maxhp, int angeldmg, int demondmg, int time) => $"ch_dm {maxhp} {angeldmg} {demondmg} {time}";

        public static string GenerateDelay(int delay, int type, string argument) => $"delay {delay} {type} {argument}";

        public static string GeneratePDelay(int delay, int type, string argument) => $"pdelay {delay} {type} {argument}";

        public static string GenerateDialog(string dialog) => $"dlg {dialog}";

        public static string GenerateFmRank(byte type, int rank, string myfamily, byte familylvl, long sum, long familyid, int FamilyExperience) => $"fmrank_stc {type} {rank}|{myfamily}|{familylvl}|{sum}|{familyid}|{FamilyExperience}";

        //public static string GenerateFrank(byte type, ClientSession session)
        //{
        //    var packet = "frank_stc";
        //    var i = 0;
        //    List<Family> familyordered = null;

        //    switch (type)
        //    {
        //        case 0:
        //        case 2:
        //        case 3:
        //            packet += " 0";
        //            break;

        //        case 4:
        //        case 6:
        //        case 7:
        //            packet += " 1";
        //            break;

        //        case 8:
        //        case 9:
        //            packet += " 2";
        //            break;

        //        default:
        //            return string.Empty;
        //    }

        //    switch (type)
        //    {
        //        case 0:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().OrderByDescending(
        //                s => s.FamilyExperience).ToList();
        //            break;

        //        case 2:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().Where(a => a.FamilyFaction == 1)
        //                .OrderByDescending(
        //                    s => s.FamilyExperience).ToList();
        //            break;

        //        case 3:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().Where(a => a.FamilyFaction == 2)
        //                .OrderByDescending(
        //                    s => s.FamilyExperience).ToList();
        //            break;

        //        case 4:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().OrderByDescending(
        //                s => s.FamilyLogs.Where(l =>
        //                        l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) < DateTime.Now)
        //                    .ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();
        //            break;

        //        case 6:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().Where(a => a.FamilyFaction == 1)
        //                .OrderByDescending(
        //                    s => s.FamilyLogs.Where(l =>
        //                            l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) < DateTime.Now)
        //                        .ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();
        //            break;

        //        case 7:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().Where(a => a.FamilyFaction == 2)
        //                .OrderByDescending(
        //                    s => s.FamilyLogs.Where(l =>
        //                            l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) < DateTime.Now)
        //                        .ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();
        //            break;

        //        case 8:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().OrderByDescending(
        //                s => s.FamilyExperience).ToList();
        //            break;

        //        case 9:
        //            familyordered = ServerManager.Instance.FamilyList.GetAllItems().OrderByDescending(
        //                s => s.FamilyCharacters.Sum(c => c.Character.Reputation)).ToList();
        //            break;
        //    }

        //    if (familyordered != null)
        //    {
        //        session.Character.GenerateFmRank(type);
        //        foreach (var fam in familyordered.Take(100))
        //        {
        //            i++;
        //            switch (type)
        //            {
        //                case 0:
        //                    packet += $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyExperience}";
        //                    break;

        //                case 2:
        //                    packet +=
        //                        $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyCharacters.Sum(c => c.Character.Act4Points)}";
        //                    break;

        //                case 3:
        //                    packet +=
        //                        $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyCharacters.Sum(c => c.Character.Act4Points)}";
        //                    break;

        //                case 4:
        //                    packet += $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyExperience}";
        //                    break;

        //                case 6:
        //                    packet +=
        //                        $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyCharacters.Sum(c => c.Character.Act4Points)}";
        //                    break;

        //                case 7:
        //                    packet +=
        //                        $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyCharacters.Sum(c => c.Character.Act4Points)}";
        //                    break;

        //                case 8:
        //                    packet += $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyExperience}";
        //                    break;

        //                case 9:
        //                    packet +=
        //                        $" {i}|{fam.Name}|{fam.FamilyLevel}|{fam.FamilyCharacters.Sum(c => c.Character.Reputation)}";
        //                    break;
        //            }
        //        }
        //    }

        //    return packet;
        //}

        public static string GenerateGuri(byte type, byte argument, long callerId, int value = 0, int value2 = 0)
        {
            switch (type)
            {
                case 2:
                    return $"guri 2 {argument} {callerId}";

                case 6:
                    return $"guri 6 1 {callerId} {value} 0";

                case 10:
                    return $"guri 10 {argument} {value} {callerId}";

                case 15:
                    return $"guri 15 {argument} 0 0";

                case 31:
                    return $"guri 31 {argument} {callerId} {value} {value2}";

                default:
                    return $"guri {type} {argument} {callerId} {value}";
            }
        }

        public static string GenerateInbox(string value) => $"inbox {value}";

        public static string GenerateInfo(string message) => $"info {message}";

        public static string GenerateMapOut() => "mapout";

        public static string GenerateModal(string message, int type) => $"modal {type} {message}";

        public static string GenerateMsg(string message, int type) => $"msg {type} {message}";

        public static string GenerateMsgi(int type, GameConstString gameConst, GameConstString constString = 0, short firstArgument = 0, short secondArgument = 0, short thirdArgument = 0, byte subType = 14)
           => $"msgi {type} {(short)gameConst} {subType} {(short)constString} {firstArgument} {secondArgument} {thirdArgument}";

        public static string GenerateMsgi2(int type, GameConstString gameConst, short unknow, string firstArgument = null, string secondArgumet = null)
            => $"msgi2 {type} {gameConst} {unknow} {firstArgument} {secondArgumet}";

        public static string GeneratePClear() => "p_clear";

        public static string GenerateRCBList(CBListPacket packet)
        {
            return BazaarHttpClient.Instance.GenerateRcbList(new GetRcbListQuery { Packet = packet });
        }

        public static string GenerateRemovePacket(short slot) => $"{slot}.-1.0.0.0";

        public static string GenerateRl(byte type)
        {
            var str = $"rl {type}";

            ServerManager.Instance.GroupList.ForEach(s =>
            {
                if (s.SessionCount > 0)
                {
                    var leader = s.Sessions.ElementAt(0);
                    str +=
                        $" {s.Raid.Id}.{s.Raid?.LevelMinimum}.{s.Raid?.LevelMaximum}.{leader.Character.Name}.{leader.Character.Level}.{(leader.Character.UseSp ? leader.Character.Morph : -1)}.{(byte)leader.Character.Class}.{(byte)leader.Character.Gender}.{s.SessionCount}.{leader.Character.HeroLevel}";
                }
            });

            return str;
        }

        public static string GenerateRp(int mapid, int x, int y, string param) => $"rp {mapid} {x} {y} {param}";

        public static string GenerateSay(string message, int type, long callerId = 0) => $"say 1 {callerId} {type} {message}";

        public static string GenerateShopMemo(int type, string message) => $"s_memo {type} {message}";

        public static string GenerateTeamArenaClose() => "ta_close";

        public static string GenerateTeamArenaMenu(byte mode, byte zenasScore, byte ereniaScore, int time,
                                                   byte arenaType) => $"ta_m {mode} {zenasScore} {ereniaScore} {time} {arenaType}";

        public static IEnumerable<string> GenerateVb()
        {
            return new[] {"vb 339 0 0", "vb 472 0 0" };
        }

        public string GenerateFStashRemove(short slot) => $"f_stash {GenerateRemovePacket(slot)}";

        public string GenerateInventoryRemove(InventoryType Type, short Slot) => $"ivn {(byte)Type} {GenerateRemovePacket(Slot)}";

        public string GeneratePStashRemove(short slot) => $"pstash {GenerateRemovePacket(slot)}";

        public string GenerateStashRemove(short slot) => $"stash {GenerateRemovePacket(slot)}";

        public string GenerateTaSt(TalentArenaOptionType watch) => $"ta_st {(byte)watch}";

        #endregion
    }
}