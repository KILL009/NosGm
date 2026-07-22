/*
 * This file is part of the NosGm Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Event;

using NosGm.GameObject.Service;
using NosGm.GameObject.Extension.Inventory;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using System.Data.Entity;
using System.Windows.Forms;
using NosGm.GameObject.Extension.Npc;
using NosGm.GameObject.Extension.Message;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Configuration;


namespace NosGm.GameObject
{
    public static class NRunHandler
    {
        #region Methods

        public static void NRun(ClientSession Session, NRunPacket packet)
        {
            if (!Session.HasCurrentMapInstance)
            {
                return;
            }

            MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == packet.NpcId);

            TeleporterDTO tp;

            var rand = new Random();
            switch (packet.Runner)
            {
                case 1:
                    if (Session.Character.Class != (byte)ClassType.Adventurer)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ADVENTURER"), 0));
                        return;
                    }
                    if (Session.Character.Level < 15 || Session.Character.JobLevel < 20)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOW_LVL"), 0));
                        return;
                    }
                    if (packet.Type > 3 || packet.Type < 1)
                    {
                        return;
                    }
                    if (packet.Type == (byte)Session.Character.Class)
                    {
                        return;
                    }
                    if (Session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                    {
                        switch (packet.Type)
                        {
                            //Swordsman
                            case 1:
                                Session.Character.Inventory.AddNewToInventory(19, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(69, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(95, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(2082, 999, InventoryType.Etc);
                                Session.Character.Inventory.AddNewToInventory(10015, 99, InventoryType.Etc);
                                Session.Character.Inventory.AddNewToInventory(10016, 99, InventoryType.Etc);
                                break;

                            //Archer
                            case 2:
                                Session.Character.Inventory.AddNewToInventory(33, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(79, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(108, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(2083, 999, InventoryType.Etc);
                                Session.Character.Inventory.AddNewToInventory(10015, 99, InventoryType.Etc);
                                Session.Character.Inventory.AddNewToInventory(10016, 99, InventoryType.Etc);
                                break;

                            //Magician
                            case 3:
                                Session.Character.Inventory.AddNewToInventory(47, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(87, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(121, 1, InventoryType.Wear, 7, 9);
                                Session.Character.Inventory.AddNewToInventory(10015, 99, InventoryType.Etc);
                                Session.Character.Inventory.AddNewToInventory(10016, 99, InventoryType.Etc);
                                break;
                        }

                        Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateEq());
                        Session.SendPacket(Session.Character.GenerateEquipment());
                        Session.SendPacket(Session.Character.GenerateSki());
                        Session.Character.ChangeClass((ClassType)packet.Type, false);
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.AddSkill(222);
                                Session.Character.AddSkill(223);
                                Session.Character.AddSkill(224);
                                Session.Character.AddSkill(225);
                                Session.Character.AddSkill(226);
                                Session.Character.AddSkill(227);
                                Session.Character.AddSkill(228);
                                Session.Character.AddSkill(229);
                                Session.Character.AddSkill(230);
                                Session.Character.AddSkill(231);
                                Session.Character.AddSkill(232);
                                Session.Character.AddSkill(233);
                                Session.Character.AddSkill(234);
                                Session.SendPacket(Session.Character.GenerateSki());
                                Session.Character.GenerateQuicklist();
                                break;

                            case ClassType.Archer:
                                Session.Character.AddSkill(242);
                                Session.Character.AddSkill(243);
                                Session.Character.AddSkill(244);
                                Session.Character.AddSkill(245);
                                Session.Character.AddSkill(246);
                                Session.Character.AddSkill(247);
                                Session.Character.AddSkill(248);
                                Session.Character.AddSkill(249);
                                Session.Character.AddSkill(250);
                                Session.Character.AddSkill(251);
                                Session.Character.AddSkill(252);
                                Session.Character.AddSkill(253);
                                Session.Character.AddSkill(254);
                                Session.Character.AddSkill(255);
                                Session.Character.AddSkill(256);
                                Session.SendPacket(Session.Character.GenerateSki());
                                Session.Character.GenerateQuicklist();
                                break;

                            case ClassType.Magician:
                                Session.Character.AddSkill(262);
                                Session.Character.AddSkill(263);
                                Session.Character.AddSkill(264);
                                Session.Character.AddSkill(265);
                                Session.Character.AddSkill(266);
                                Session.Character.AddSkill(267);
                                Session.Character.AddSkill(268);
                                Session.Character.AddSkill(269);
                                Session.Character.AddSkill(270);
                                Session.Character.AddSkill(271);
                                Session.Character.AddSkill(272);
                                Session.Character.AddSkill(273);
                                Session.Character.AddSkill(274);
                                Session.Character.AddSkill(275);
                                Session.Character.AddSkill(276);
                                Session.Character.AddSkill(277);
                                Session.SendPacket(Session.Character.GenerateSki());
                                Session.Character.GenerateQuicklist();
                                break;
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                    }
                    break;

                case 2:
                    Session.SendPacket("wopen 1 0");
                    break;

                case 3:
                    NpcMonster heldMonster = ServerManager.GetNpcMonster(packet.Type);
                    if (heldMonster != null && !Session.Character.Mates.Any(m => m.NpcMonsterVNum == heldMonster.NpcMonsterVNum && !m.IsTemporalMate) && Session.Character.Mates.FirstOrDefault(s => s.NpcMonsterVNum == heldMonster.NpcMonsterVNum && s.IsTemporalMate && s.IsTsReward) is Mate partnerToReceive)
                    {
                        Session.Character.RemoveTemporalMates();
                        Mate partner = new Mate(Session.Character, heldMonster, (byte)heldMonster.Level, MateType.Partner);
                        partner.Experience = partnerToReceive.Experience;
                        if (!Session.Character.Mates.Any(s => s.MateType == MateType.Partner && s.IsTeamMember))
                        {
                            partner.IsTeamMember = true;
                        }
                        Session.Character.AddPet(partner);
                    }
                    break;

                case 4:
                    Mate mate = Session.Character.Mates.Find(s => s.MateTransportId == packet.NpcId);
                    switch (packet.Type)
                    {
                        case 2:
                            if (mate != null)
                            {
                                if (Session.Character.Miniland == Session.Character.MapInstance)
                                {
                                    if (Session.Character.Level >= mate.Level)
                                    {
                                        Mate teammate = Session.Character.Mates.Where(s => s.IsTeamMember).FirstOrDefault(s => s.MateType == mate.MateType);
                                        if (teammate != null)
                                        {
                                            teammate.RemoveTeamMember();
                                        }
                                        mate.AddTeamMember();
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PET_HIGHER_LEVEL"), 0));
                                    }
                                }
                            }
                            break;

                        case 3:
                            if (mate != null && Session.Character.Miniland == Session.Character.MapInstance)
                            {
                                mate.RemoveTeamMember();
                            }
                            break;

                        case 4:
                            if (mate != null)
                            {
                                if (Session.Character.Miniland == Session.Character.MapInstance)
                                {
                                    mate.RemoveTeamMember(false);
                                    mate.MapX = mate.PositionX;
                                    mate.MapY = mate.PositionY;
                                }
                                else
                                {
                                    Session.SendPacket($"qna #n_run^4^5^3^{mate.MateTransportId} {Language.Instance.GetMessageFromKey("ASK_KICK_PET")}");
                                }
                                break;
                            }
                            break;

                        case 5:
                            if (mate != null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 10, $"#n_run^4^6^3^{mate.MateTransportId}"));
                            }
                            break;

                        case 6:
                            if (mate != null && Session.Character.Miniland != Session.Character.MapInstance)
                            {
                                mate.BackToMiniland();
                                #region Remove Partner Buff
                                Session.Character.RemoveBuff(3000);
                                Session.Character.RemoveBuff(3001);
                                Session.Character.RemoveBuff(3002);
                                Session.Character.RemoveBuff(3003);
                                Session.Character.RemoveBuff(3004);
                                Session.Character.RemoveBuff(3005);
                                Session.Character.RemoveBuff(3006);
                                Session.Character.RemoveBuff(3007);
                                Session.Character.RemoveBuff(3008);
                                Session.Character.RemoveBuff(3009);
                                Session.Character.RemoveBuff(3010);
                                Session.Character.RemoveBuff(3011);
                                Session.Character.RemoveBuff(3012);
                                Session.Character.RemoveBuff(3013);
                                Session.Character.RemoveBuff(3014);
                                Session.Character.RemoveBuff(3015);
                                Session.Character.RemoveBuff(3016);
                                Session.Character.RemoveBuff(3017);
                                Session.Character.RemoveBuff(3018);
                                Session.Character.RemoveBuff(3019);
                                Session.Character.RemoveBuff(3020);
                                Session.Character.RemoveBuff(3021);
                                Session.Character.RemoveBuff(3022);
                                Session.Character.RemoveBuff(3023);
                                Session.Character.RemoveBuff(3024);
                                Session.Character.RemoveBuff(3025);
                                Session.Character.RemoveBuff(3026);
                                Session.Character.RemoveBuff(3027);
                                Session.Character.RemoveBuff(3028);
                                Session.Character.RemoveBuff(3029);
                                Session.Character.RemoveBuff(3030);
                                Session.Character.RemoveBuff(3031);
                                Session.Character.RemoveBuff(3032);
                                Session.Character.RemoveBuff(3033);
                                Session.Character.RemoveBuff(3034);
                                #endregion
                            }
                            break;

                        case 7:
                            if (mate != null)
                            {
                                if (Session.Character.Mates.Any(s => s.MateType == mate.MateType && s.IsTeamMember))
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ALREADY_PET_IN_TEAM"), 11));
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ALREADY_PET_IN_TEAM"), 0));
                                }
                                else
                                {
                                    mate.RemoveTeamMember();
                                    Session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 10, $"#n_run^4^9^3^{mate.MateTransportId}"));
                                }
                            }
                            break;

                        case 9:
                            if (mate != null && mate.IsSummonable && Session.Character.MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                            {
                                if (Session.Character.Level >= mate.Level)
                                {
                                    mate.PositionX = (short)(Session.Character.PositionX + (mate.MateType == MateType.Partner ? -1 : 1));
                                    mate.PositionY = (short)(Session.Character.PositionY + 1);
                                    mate.AddTeamMember();
                                    Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                                        {
                                            s.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                        }
                                        else
                                        {
                                            s.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                        }
                                    });
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PET_HIGHER_LEVEL"), 0));
                                }
                            }
                            break;
                    }
                    Session.SendPacket(Session.Character.GeneratePinit());
                    Session.SendPackets(Session.Character.GeneratePst());
                    break;

                case 6:
                    break;

                case 10:
                    Session.SendPacket("wopen 3 0");
                    break;

                case 12:
                    Session.SendPacket($"wopen {packet.Type} 0");
                    break;

                case 14:
                    Session.SendPacket("wopen 27 0");
                    string recipelist = "m_list 2";
                    if (npc != null)
                    {
                        List<Recipe> tps = npc.Recipes;
                        recipelist = tps.Where(s => s.Amount > 0).Aggregate(recipelist, (current, s) => current + $" {s.ItemVNum}");
                        recipelist += " -100";
                        Session.SendPacket(recipelist);
                    }
                    break;

                case 15:
                    if (npc != null)
                    {
                        if (packet.Value == 2)
                        {
                            Session.SendPacket($"qna #n_run^15^1^1^{npc.MapNpcId} {Language.Instance.GetMessageFromKey("ASK_CHANGE_SPAWNLOCATION")}");
                        }
                        else
                        {
                            switch (npc.MapId)
                            {
                                case 1:
                                    Session.Character.SetRespawnPoint(1, 79, 116);
                                    break;

                                case 20:
                                    Session.Character.SetRespawnPoint(20, 9, 92);
                                    break;

                                case 145:
                                    Session.Character.SetRespawnPoint(145, 13, 110);
                                    break;

                                case 170:
                                    Session.Character.SetRespawnPoint(170, 79, 47);
                                    break;

                                case 177:
                                    Session.Character.SetRespawnPoint(177, 149, 74);
                                    break;

                                case 189:
                                    Session.Character.SetRespawnPoint(189, 58, 166);
                                    break;

                                case 228:
                                    Session.Character.SetRespawnPoint(228, 80, 98);
                                    break;
                            }
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RESPAWNLOCATION_CHANGED"), 0));
                        }
                    }
                    break;

                case 16:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (packet.Type >= 0 && Session.Character.Gold >= 1000 * packet.Type)
                        {
                            Session.Character.Gold -= 1000 * packet.Type;
                            Session.SendPacket(Session.Character.GenerateGold());
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-2, 2)), (short)(tp.MapY + ServerManager.RandomNumber(2, 2)));
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 17:
                    switch (packet.Type)
                    {
                        case 0:
                            if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                            {
                                return;
                            }
                            if (packet.Value == 1)
                            {
                                Session.SendPacket($"qna #n_run^{packet.Runner}^{packet.Type}^2^{packet.NpcId} {string.Format(Language.Instance.GetMessageFromKey("ASK_ENTER_GOLD_NORM"), 500 * (1 + packet.Type))}");
                            }
                            else
                            {
                                double currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                                double timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
                                if (!(timeSpanSinceLastPortal >= 4) || !Session.HasCurrentMapInstance || ServerManager.Instance.ChannelId == 51 || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                                    return;
                                }
                                if (packet.Type >= 0 && Session.Character.Gold >= 500 * (1 + packet.Type))
                                {
                                    Session.Character.LastPortal = currentRunningSeconds;
                                    Session.Character.Gold -= 500 * (1 + packet.Type);
                                    Session.SendPacket(Session.Character.GenerateGold());
                                    Session.SendPacket(Session.Character.GenerateAscr());
                                    Session.SendPacket("msg 3 PvP will be available in 5 Seconds.");
                                    MapCell pos = packet.Type == 0 ? ServerManager.Instance.ArenaInstance.Map.GetRandomPosition() : ServerManager.Instance.FamilyArenaInstance.Map.GetRandomPosition();
                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, packet.Type == 0 ? ServerManager.Instance.ArenaInstance.MapInstanceId : ServerManager.Instance.FamilyArenaInstance.MapInstanceId, pos.X, pos.Y);
                                }
                                else
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                }
                            }
                            break;

                        case 1:
                            if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                            {
                                return;
                            }
                            if (packet.Value == 1)
                            {
                                Session.SendPacket($"qna #n_run^{packet.Runner}^{packet.Type}^2^{packet.NpcId} {string.Format(Language.Instance.GetMessageFromKey("ASK_ENTER_GOLD_FAM"), 1000 * (1 + packet.Type))}");
                            }
                            else
                            {
                                double currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                                double timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
                                if (!(timeSpanSinceLastPortal >= 4) || !Session.HasCurrentMapInstance || ServerManager.Instance.ChannelId == 51 || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                                    return;
                                }
                                if (packet.Type >= 0 && Session.Character.Gold >= 1000 * (1 + packet.Type))
                                {
                                    Session.Character.LastPortal = currentRunningSeconds;
                                    Session.Character.Gold -= 1000 * (1 + packet.Type);
                                    Session.SendPacket(Session.Character.GenerateGold());
                                    Session.SendPacket(Session.Character.GenerateAscr());
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PVP_ACTIVED_ON_MAP"), 10));
                                    MapCell pos = packet.Type == 0 ? ServerManager.Instance.ArenaInstance.Map.GetRandomPosition() : ServerManager.Instance.FamilyArenaInstance.Map.GetRandomPosition();
                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, packet.Type == 0 ? ServerManager.Instance.ArenaInstance.MapInstanceId : ServerManager.Instance.FamilyArenaInstance.MapInstanceId, pos.X, pos.Y);
                                }
                                else
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                }
                            }
                            break;
                    }
                    break;

                case 18:
                    if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                    {
                        return;
                    }
                    Session.SendPacket(Session.Character.GenerateNpcDialog(17));
                    break;

                case 23:
                    if (packet.Type == 0)
                    {
                        if (Session.Character.Group?.SessionCount == 3)
                        {
                            foreach (ClientSession s in Session.Character.Group.Sessions.GetAllItems())
                            {
                                if (s.Character.Family != null)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_MEMBER_ALREADY_IN_FAMILY")));
                                    return;
                                }
                            }
                        }
                        if (Session.Character.Group == null || Session.Character.Group.SessionCount != 3)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("FAMILY_GROUP_NOT_FULL")));
                            return;
                        }
                        Session.SendPacket(UserInterfaceHelper.GenerateInbox($"#glmk^ {14} 1 {Language.Instance.GetMessageFromKey("CREATE_FAMILY").Replace(' ', '^')}"));
                    }
                    else
                    {
                        if (Session.Character.Family == null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_IN_FAMILY")));
                            return;
                        }
                        if (Session.Character.Family != null && Session.Character.FamilyCharacter != null && Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_FAMILY_HEAD")));
                            return;
                        }
                        Session.SendPacket($"qna #glrm^1 {Language.Instance.GetMessageFromKey("DISSOLVE_FAMILY")}");
                    }

                    break;

                case 26:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.Gold >= 5000 * packet.Type && packet.Type >= 0)
                        {
                            Session.Character.Gold -= 5000 * packet.Type;
                            Session.SendPacket(Session.Character.GenerateGold());
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-3, 3)), (short)(tp.MapY + ServerManager.RandomNumber(-3, 3)));
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 45:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.Gold >= 500)
                        {
                            Session.Character.Gold -= 500;
                            Session.SendPacket(Session.Character.GenerateGold());
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-3, 3)), (short)(tp.MapY + ServerManager.RandomNumber(-3, 3)));
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 60:
                    {
                        if (!Session.Character.CanUseNosBazaar())
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                            return;
                        }

                        MedalType medalType = 0;
                        int time = 0;

                        StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

                        if (medal != null)
                        {
                            time = (int)(medal.DateEnd - DateTime.Now).TotalHours;

                            switch (medal.StaticBonusType)
                            {
                                case StaticBonusType.BazaarMedalGold:
                                    medalType = MedalType.Gold;
                                    break;
                                case StaticBonusType.BazaarMedalSilver:
                                    medalType = MedalType.Silver;
                                    break;
                            }
                        }

                        Session.SendPacket($"wopen 32 {(byte)medalType} {time}");
                    }
                    break;


                /*
                  * [The Flaming Sword]
                  * Daily Quest = 65,
                  * Restore Seal = 61,
                  * Rune Piece = 62;
                  * 
                  * 
                  * [Heroes of Fire]
                  * Heroes of Fire = 110,
                  * Make Raid Seal = 111,
                  * Exchange SP = 145,
                  * Exchange for Perfection Stones = 146;
                  * 
                  * 
                  * [Heroes of Ice]
                  * Heroes of Ice = 131,
                  * Create Raid Seal = 133,
                  * Exchange for SP = 147,
                  * Exchange for Perfection Stones = 148;
                */

                //Iceflower Quest
                case 65:
                    const int ItemNeeded = 5911;
                    if (Session.Character.Level > 90 && Session.Character.Level <= 99 && Session.Character.Inventory.CountItem(ItemNeeded) >= 5)
                    {
                        Session.Character.GiftAdd(5929, 20);
                        Session.Character.Inventory.RemoveItemAmount(ItemNeeded, 5);
                        Session.Character.HasDoneIceFlowerQuest = true;
                    }
                    else if (Session.Character.Level > 1 && Session.Character.Level <= 89)
                    {
                        int LevelNeeded = 90;
                        Session.SendPacket(Session.Character.GenerateSay($"Error! Your Level is not high enough. Your Level must be: {LevelNeeded}", 11));
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Ice Flowers! Amount required: 5", 11));
                    }
                    break;

                case 61:
                    const int ItemNeeded1 = 5917;
                    const int ItemNeeded2 = 5918;
                    if (Session.Character.Inventory.CountItem(ItemNeeded1) >= 1 && Session.Character.Inventory.CountItem(ItemNeeded2) >= 1)
                    {
                        Session.Character.GiftAdd(5922, 1);
                        Session.Character.Inventory.RemoveItemAmount(ItemNeeded1, 1);
                        Session.Character.Inventory.RemoveItemAmount(ItemNeeded2, 1);
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay("You need at least 1x Left Side of Grenigas' Raid Seal and 1x Right Side of Grenigas' Raid Seal", 11));
                    }
                    break;

                case 62:
                    if (Session.Character.Level > 89 && Session.Character.Level <= 99)
                    {
                        const int ItemNeeded3 = 5919;
                        if (Session.Character.Inventory.CountItem(ItemNeeded3) >= 1)
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2536, 26, 42);
                            Session.Character.Inventory.RemoveItemAmount(ItemNeeded3, 1);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You need at least 1x Rune Piece", 11));
                        }
                    }
                    else if (Session.Character.Level > 1 && Session.Character.Level <= 89)
                    {
                        int LevelNeeded = 90;
                        Session.SendPacket(Session.Character.GenerateSay($"Error! Your Level is not high enough. Your Level must be: {LevelNeeded}", 11));
                    }
                    break;


                case 66:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5914);
                        }
                    }
                    break;

                case 67:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5908);
                        }
                    }
                    break;

                case 68:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5919);
                        }
                    }
                    break;

                case 69:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 5)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5929, 10);
                        Session.Character.Inventory.RemoveItemAmount(5910, 5);
                    }
                    break;

                case 70:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 90)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5923, 1);
                        Session.Character.Inventory.RemoveItemAmount(5910, 90);
                    }
                    break;

                case 71:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 300)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5914, 1);
                        Session.Character.Inventory.RemoveItemAmount(5910, 300);
                    }
                    break;


                case 110:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5954);
                        }
                    }
                    break;

                case 111:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1012) < 20 || Session.Character.Inventory.CountItem(1013) < 20 || Session.Character.Inventory.CountItem(1027) < 20)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5500, 1);
                        Session.Character.Inventory.RemoveItemAmount(1012, 20);
                        Session.Character.Inventory.RemoveItemAmount(1013, 20);
                        Session.Character.Inventory.RemoveItemAmount(1027, 20);
                    }
                    break;


                case 131:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5982);
                        }
                    }
                    break;

                case 132:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-3, 3)), (short)(tp.MapY + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                case 133:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1012) < 20 || Session.Character.Inventory.CountItem(2307) < 20 || Session.Character.Inventory.CountItem(5911) < 20)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5512, 1);
                        Session.Character.Inventory.RemoveItemAmount(1012, 20);
                        Session.Character.Inventory.RemoveItemAmount(2307, 20);
                        Session.Character.Inventory.RemoveItemAmount(5911, 20);
                    }
                    break;

                case 134:
                    if (npc == null || !Session.Character.Quests.Any(s => s.Quest.QuestObjectives.Any(o => o.SpecialData == 5518)))
                    {
                        return;
                    }
                    short vNum = 0;
                    for (short i = 4494; i <= 4496; i++)
                    {
                        if (Session.Character.Inventory.CountItem(i) > 0)
                        {
                            vNum = i;
                            break;
                        }
                    }
                    if (vNum > 0)
                    {
                        Session.Character.GiftAdd(5518, 1);
                        Session.Character.GiftAdd(4504, 1);
                        Session.Character.Inventory.RemoveItemAmount(vNum, 1);
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                    }
                    break;

                case 135:
                    if (!ServerManager.Instance.StartedEvents.Contains(EventType.TALENTARENA))
                    {
                        TimeSpan time = ServerManager.Instance.Schedules.ToList().FirstOrDefault(s => s.Event == EventType.TALENTARENA)?.Time ?? TimeSpan.FromSeconds(0);
                        Session.SendPacket(npc?.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_NOT_OPEN"), string.Format("{0:D2}:{1:D2} - {2:D2}:{3:D2}", time.Hours, time.Minutes, (time.Hours + 4) % 24, time.Minutes)), 10));
                    }
                    else
                    {
                        if (Session.Character.Level < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LOW_LVL_30")));
                            return;
                        }

                        var tickets = 20;

                        if (tickets > 0)
                        {
                            if (ServerManager.Instance.ArenaMembers.ToList().All(s => s.Session != Session))
                            {
                                if (ServerManager.Instance.IsCharacterMemberOfGroup(Session.Character.CharacterId))
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TALENT_ARENA_GROUP"), 0));
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TALENT_ARENA_GROUP"), 10));
                                }
                                else
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_TICKET_LEFT"), tickets), 10));

                                    ServerManager.Instance.ArenaMembers.Add(new ArenaMember
                                    {
                                        ArenaType = EventType.TALENTARENA,
                                        Session = Session,
                                        GroupId = null,
                                        Time = 0
                                    });
                                }
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TALENT_ARENA_NO_MORE_TICKET"), 0));
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TALENT_ARENA_NO_MORE_TICKET"), 10));
                        }
                    }
                    break;

                case 137:
                    Session.SendPacket("taw_open");
                    break;

                case 138:
                    ConcurrentBag<ArenaTeamMember> at = ServerManager.Instance.ArenaTeams.ToList().Where(s => s.Any(c => c.Session?.CurrentMapInstance != null)).OrderBy(s => rand.Next()).FirstOrDefault();
                    if (at != null)
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, at.FirstOrDefault().Session.CurrentMapInstance.MapInstanceId, (short)(69 + ServerManager.RandomNumber(-2, 2)), (short)(100 + ServerManager.RandomNumber(-2, 2)));

                        var zenas = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ZENAS);
                        var erenia = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ERENIA);
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaM(0));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaM(3));
                        Session.SendPacket("taw_sv 0");
                        Session.SendPacket(zenas?.Session?.Character?.GenerateTaP(0, true));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaP(2, true));
                        Session.SendPacket(zenas?.Session?.Character?.GenerateTaFc(0));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaFc(1));
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NO_TEAM_ARENA")));
                    }

                    break;


                case 19:
                case 144:
                    if (Session.Character.Timespace != null)
                    {
                        if (Session.Character.MapInstance.InstanceBag.EndState == 10)
                        {
                            EventHelper.Instance.RunEvent(new EventContainer(Session.Character.MapInstance, EventActionType.SCRIPTEND, (byte)5));
                        }
                    }
                    break;

                case 145:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2522) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.GiftAdd(4500, 1);
                                break;

                            case ClassType.Archer:
                                Session.Character.GiftAdd(4501, 1);
                                break;

                            case ClassType.Magician:
                                Session.Character.GiftAdd(4502, 1);
                                break;
                        }
                        Session.Character.Inventory.RemoveItemAmount(2522, 50);
                    }
                    break;

                case 146:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2522) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(2518, 1);
                        Session.Character.Inventory.RemoveItemAmount(2522, 50);
                    }
                    break;

                case 147:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2523) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.GiftAdd(4497, 1);
                                break;

                            case ClassType.Archer:
                                Session.Character.GiftAdd(4498, 1);
                                break;

                            case ClassType.Magician:
                                Session.Character.GiftAdd(4499, 1);
                                break;
                        }
                        Session.Character.Inventory.RemoveItemAmount(2523, 50);
                    }
                    break;

                case 148:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2523) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(2519, 1);
                        Session.Character.Inventory.RemoveItemAmount(2523, 50);
                    }
                    break;


                case 193:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6021);
                        }
                    }
                    break;

                case 194:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5986) < 3)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5984, 3);
                        Session.Character.Inventory.RemoveItemAmount(5986, 3);
                    }
                    break;

                case 195:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5987) < 5)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5977, 2);
                        Session.Character.Inventory.RemoveItemAmount(5987, 5);
                    }
                    break;

                //Raphael the turttle first quest : Summer Event quest
                case 197:
                    {
                        if (npc != null)
                        {
                            //  Session.Character.AddQuest(notKnown);
                        }
                    }
                    break;

                case 200:
                    {
                        if (npc != null)
                        {
                            if (Session.Character.Quests.Any(s => s.Quest.QuestType == (int)QuestType.Dialog2 && s.Quest.QuestObjectives.Any(b => b.Data == npc.NpcVNum)))
                            {
                                Session.Character.AddQuest(packet.Type);
                                Session.Character.IncrementQuests(QuestType.Dialog2, npc.NpcVNum);
                            }
                        }
                    }
                    break;

                //Raphael the turttle second quest :  Summer Event quest
                case 201:
                    {
                        if (npc != null)
                        {
                            //  Session.Character.AddQuest(notKnown);
                        }
                    }
                    break;

                case 300:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6040);
                        }
                    }
                    break;

                case 301:
                    if (Session.Character.Level >= 90)
                    {
                        if (Session.Character.HeroLevel == 0)
                        {
                            Session.Character.HeroLevel++;
                        }
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 228, (short)(68 + ServerManager.RandomNumber(-3, 3)), (short)(102 + ServerManager.RandomNumber(-3, 3)));
                    }
                    else
                    {
                        MessageExtension.SendInfo(Session, "You need to be at least Level 90 in order to enter Cylloan");
                    }
                    break;

                case 305:
                    if (npc != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2526, 32, 40);
                    }
                    break;

                case 306:
                    if (Session.Character.MapId == 2526)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2527, (short)(29 + ServerManager.RandomNumber(-3, 3)), (short)(43 + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                case 307:
                    if (Session.Character.MapId == 2527)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2526, (short)(32 + ServerManager.RandomNumber(-2, 2)), (short)(42 + ServerManager.RandomNumber(-2, 2)));
                    }
                    break;

                //todo : add frigg & ragnar pnj
                case 308:
                    if (npc != null & Session.Character.Level >= 50)
                    {
                        Session.Character.AddQuest(6134);
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LEVEL_50_REQUIERED_QUEST"), 0));
                    }
                    break;

                case 312:
                    if (npc != null)
                    {
                        Session.Character.AddQuest(6208);
                    }
                    break;

                case 313:
                    if (npc != null && Session.Character.Gold >= 50000)
                    {
                        Session.Character.Gold -= 50000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 261, (short)(181 + ServerManager.RandomNumber(-3, 3)), (short)(212 + ServerManager.RandomNumber(-3, 3)));
                    }
                    else if (Session.Character.Gold < 50000)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                    }
                    break;

                case 314:
                    if (npc != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, (short)(52 + ServerManager.RandomNumber(-3, 3)), (short)(40 + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                //Jennifer quest
                case 315:
                    if (npc != null)
                    {
                        Session.Character.AddQuest(6242);
                    }
                    break;

                //Professor macavity
                case 316:
                    if (npc != null)
                    {
                        Session.Character.AddQuest(6242);
                    }
                    break;

                //Malcolm 10th anniversary 
                case 319:
                    if (npc != null)
                    {

                    }
                    break;

                case 322://Dialog = 356
                    {
                        if (packet.Type == 0 && packet.Value == 2)
                        {
                            var Item = Session.Character.Inventory.CountItem(5836);
                            if (Item == 0)
                            {
                                var iteminfo = ServerManager.GetItem(5836);
                                var inv = Session.Character.Inventory.AddNewToInventory(5836).FirstOrDefault();
                                if (inv != null)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BANK_BOOK_RECEIVED")));
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                            else
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateSay(Language.Instance.GetMessageFromKey("ALREADY_RECEIVED_BANK_BOOK"), 10));
                            }
                        }
                    }
                    break;

                case 323: // Guard quest from Tart Hapendam
                    {
                        if (npc != null)
                        {
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateSay(Language.Instance.GetMessageFromKey(""), 10));
                        }
                    }
                    break;

                case 324: // MA Quest SP2
                    {
                        if (npc != null && Session.Character.Class == ClassType.MartialArtist)
                        {
                            Session.Character.AddQuest(6307);
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateSay(Language.Instance.GetMessageFromKey("QUEST_ONLY_FOR_MARTIAL_ARTIST"), 10));
                        }
                    }
                    break;


                //First quest act7
                case 332:
                    {
                        if (npc != null && Session.Character.HeroLevel >= 30 && Session.Character.Level >= 90)
                        {
                            Session.Character.AddQuest(6500);
                        }
                        else
                        {
                            //Add key : Your character level is too low
                        }
                    }
                    break;

                //Quest sp4 martial artist
                case 333:
                    {
                        if (npc != null && Session.Character.Class == ClassType.MartialArtist)
                        {
                            Session.Character.AddQuest(6346);
                        }
                        else
                        {
                            //Add key : Your character level is too low
                            Session.SendPacket(UserInterfaceHelper.GenerateSay(Language.Instance.GetMessageFromKey("QUEST_ONLY_FOR_MARTIAL_ARTIST"), 10));
                        }
                    }
                    break;

                //Tp Desert to act7
                case 334:
                    if (npc != null)
                    {
                        MapInstance map4 = null;
                        switch (Session.Character.Faction)
                        {
                            case FactionType.None:
                            case FactionType.Angel:
                            case FactionType.Demon:
                                map4 = ServerManager.GetAllMapInstances().Find(s => s.MapInstanceType.Equals(MapInstanceType.Act7Ship));
                                break;
                        }
                        if (map4 == null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHIP_NOTARRIVED"), 0));
                            return;
                        }
                        if (Session.Character.Level < 90)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ACT7_REQUIERED_LEVEL_TOO_LOW"), 0));
                            return;
                        }
                        if (Session.Character.Gold < 25000)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        Session.Character.Gold -= 25000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        MapCell pos = map4.Map.GetRandomPosition();
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, map4.MapInstanceId, pos.X, pos.Y);
                    }
                    break;

                //Guardswoman Celestial Spire Ground Entrance tp
                case 335:
                    if (npc != null)
                    {
                        // dialog 11028 test it and change mapid mapy mapy
                        // ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2643, 52, 40);
                    }
                    break;

                //Alveus to act7
                case 336:
                    if (npc != null && packet.Type > 0)
                    {
                        if (Session.Character.Level < 90 || Session.Character.HeroLevel <= 30)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ACT7_REQUIERED_LEVEL_TOO_LOW"), 0));
                            return;
                        }
                        if (Session.Character.Gold < 30000 && packet.Type == 145)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        if (Session.Character.Gold < 25000 && packet.Type == 170)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        if (packet.Type == 145)
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, (short)(55 + ServerManager.RandomNumber(-2, 2)), (short)(28 + ServerManager.RandomNumber(-2, 2)));
                            Session.Character.Gold -= 30000;
                            Session.SendPacket(Session.Character.GenerateGold());
                        }
                        if (packet.Type == 170)
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 170, (short)(123 + ServerManager.RandomNumber(-3, 3)), (short)(83 + ServerManager.RandomNumber(-3, 3)));
                            Session.Character.Gold -= 25000;
                            Session.SendPacket(Session.Character.GenerateGold());
                        }
                    }
                    break;

                //Battle Tower
                case 338:
                    if (npc != null)
                    {
                        Session.SendPacket($"rbr2 {800 + Session.Character.Stage}.5.0 2 15 90.99 1 9182.{Session.Character.Stage} 2481.{Session.Character.Stage} -1.0 -1.0 -1.0 9181.1 -1.0 -1.0 -1.0 -1.0 0.- 1 0 695.90 717.90 690.90 775.90");
                    }
                    break;

                case 340: // MA Quest SP3
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6332);
                        }
                    }
                    break;

                case 666: // Hero Equipment Downgrade
                    {
                        // 4949 ~ 4966 = c25/c28
                        // 4978 ~ 4986 = c45/c48

                        const long price = 10000000;

                        ItemInstance itemInstance = Session?.Character?.Inventory?.LoadBySlotAndType(0, InventoryType.Equipment);

                        if (itemInstance?.Item != null && ((itemInstance.ItemVNum >= 4949 && itemInstance.ItemVNum <= 4966) || (itemInstance.ItemVNum >= 4978 && itemInstance.ItemVNum <= 4986)) && itemInstance.Rare == 8)
                        {
                            if (Session.Character.Gold < price)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }

                            Session.Character.Gold -= price;
                            Session.SendPacket(Session.Character.GenerateGold());

                            itemInstance.RarifyItem(Session, RarifyMode.HeroEquipmentDowngrade, RarifyProtection.None);

                            Session.SendPacket(itemInstance.GenerateInventoryAdd());
                        }
                    }
                    break;

                case 1000:
                    if (npc == null)
                    {
                        return;
                    }

                    if (Session.Character.Quests.Any(s => s.Quest.DialogNpcVNum == npc.NpcVNum && s.Quest.QuestObjectives.Any(o => o.SpecialData == packet.Type)))
                    {
                        if (ServerManager.Instance.TimeSpaces.FirstOrDefault(s => s.QuestTimeSpaceId == packet.Type) is ScriptedInstance timeSpace)
                        {
                            //Session.Character.EnterInstance(timeSpace);
                        }
                    }

                    break;

                case 1500:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(2255);
                        }
                    }
                    break;


                case 2000:
                    {
                        if (npc != null)
                        {
                            if (packet.Type == 2000 && npc.NpcVNum == 932 && !Session.Character.Quests.Any(s => s.QuestId >= 2000 && s.QuestId <= 2007) // Pajama
                                || packet.Type == 2008 && npc.NpcVNum == 933 && !Session.Character.Quests.Any(s => s.QuestId >= 2008 && s.QuestId <= 2013) // SP 1
                                || packet.Type == 2014 && npc.NpcVNum == 934 && !Session.Character.Quests.Any(s => s.QuestId >= 2014 && s.QuestId <= 2020) // SP 2
                                || packet.Type == 2060 && npc.NpcVNum == 948 && !Session.Character.Quests.Any(s => s.QuestId >= 2060 && s.QuestId <= 2095) // SP 3
                                || packet.Type == 2100 && npc.NpcVNum == 954 && !Session.Character.Quests.Any(s => s.QuestId >= 2100 && s.QuestId <= 2134) // SP 4
                                || packet.Type == 2030 && npc.NpcVNum == 422 && !Session.Character.Quests.Any(s => s.QuestId >= 2030 && s.QuestId <= 2046)
                                || packet.Type == 2048 && npc.NpcVNum == 303 && !Session.Character.Quests.Any(s => s.QuestId >= 2048 && s.QuestId <= 2050))
                            {
                                Session.Character.AddQuest(packet.Type);
                            }
                        }
                    }
                    break;

                case 2001:
                    {
                        switch (packet.Type)
                        {
                            case 1: // Pajama
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 932))
                                    {
                                        //Session.Character.GiftAdd(900, 1);
                                        return;
                                    }
                                }
                                break;
                            case 2: // SP 1
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 933))
                                    {
                                        switch (Session.Character.Class)
                                        {
                                            case ClassType.Swordsman:
                                                // Session.Character.GiftAdd(901, 1);
                                                break;
                                            case ClassType.Archer:
                                                //Session.Character.GiftAdd(903, 1);
                                                break;
                                            case ClassType.Magician:
                                                // Session.Character.GiftAdd(905, 1);
                                                break;
                                        }
                                        return;
                                    }
                                }
                                break;
                            case 3: // SP 2
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 934))
                                    {
                                        switch (Session.Character.Class)
                                        {
                                            case ClassType.Swordsman:
                                                //Session.Character.GiftAdd(902, 1);
                                                break;
                                            case ClassType.Archer:
                                                //Session.Character.GiftAdd(904, 1);
                                                break;
                                            case ClassType.Magician:
                                                // Session.Character.GiftAdd(906, 1);
                                                break;
                                        }
                                        return;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case 5:
                    if (packet.Type == 0 && packet.Value == 1)
                    {
                        if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 948 /* SP 3 */ || s.NpcVNum == 954 /* SP 4 */))
                        {
                            //SP 3
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    // Session.Character.GiftAdd(909, 1);
                                    break;
                                case ClassType.Archer:
                                    // Session.Character.GiftAdd(911, 1);
                                    break;
                                case ClassType.Magician:
                                    //  Session.Character.GiftAdd(913, 1);
                                    break;
                            }

                            //SP 4
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    // Session.Character.GiftAdd(910, 1);
                                    break;
                                case ClassType.Archer:
                                    // Session.Character.GiftAdd(912, 1);
                                    break;
                                case ClassType.Magician:
                                    // Session.Character.GiftAdd(914, 1);
                                    break;
                            }
                            return;
                        }
                    }
                    break;

                case 2002:
                    if (npc != null)
                    {
                        int gemNpcVnum = 0;

                        switch (npc.NpcVNum)
                        {
                            case 935:
                                gemNpcVnum = 932;
                                break;
                            case 936:
                                gemNpcVnum = 933;
                                break;
                            case 937:
                                gemNpcVnum = 934;
                                break;
                            case 952:
                                gemNpcVnum = 948;
                                break;
                            case 953:
                                gemNpcVnum = 954;
                                break;
                        }

                        if (ServerManager.Instance.SpecialistGemMapInstances?.FirstOrDefault(s => s.Npcs.Any(n => n.NpcVNum == gemNpcVnum)) is MapInstance specialistGemMapInstance)
                        {
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, specialistGemMapInstance.MapInstanceId, (short)(7 + ServerManager.RandomNumber(-3, 2)), (short)(5 + ServerManager.RandomNumber(-2, 3))); ;
                        }
                    }
                    break;

                case 3000:
                    {
                        if (Session.Character.Level == 15 && npc.NpcVNum == 20010)
                        {
                            Session.Character.AddQuest(1997, true);
                        }
                    }
                    break;

                ////To Act4
                //case 5001:
                //    if (CommunicationServiceClient.Instance.IsAct4Online())
                //    {
                //        //if(ServerConfiguration.Port5100MaintenanceMode && AuthorityType.GM > Session.Account.Authority )
                //        //    MessageExtension.SendYellow(Session, "Act4 is in Maintenance-Mode");

                //        if (Session.Character.Gold > 3000)
                //        {
                //            Session.Character.Gold -= 3000;
                //            Session.SendPacket(Session.Character.GenerateGold());
                //            MessageExtension.SendYellow(Session, "The Ship-Fee is 3000 Gold");
                //            Session.ReceivePacket("$Act4");
                //        }
                //        else
                //        {
                //            MessageExtension.SendYellow(Session, "I pity you poor fellow, hop on");
                //        }
                //        Session.ReceivePacket("$Act4");
                //    }
                //    else
                //        MessageExtension.SendYellow(Session, "Act4 is closed, come back later");

                //    break;


                //Act4
                case 5001:
                    if(npc != null)
                    {
                        MapInstance map = null;
                        switch(Session.Character.Faction)
                        {
                            case FactionType.None:
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to have a Faction to Join Act4!"));
                                break;

                            case FactionType.Angel:
                                map = ServerManager.GetAllMapInstances().Find(s => s.MapInstanceType.Equals(MapInstanceType.Act4ShipAngel));
                                break;

                            case FactionType.Demon:
                                map = ServerManager.GetAllMapInstances().Find(s => s.MapInstanceType.Equals(MapInstanceType.Act4ShipDemon));
                                break;
                        }
                        if(map == null || npc.EffectActivated)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHIP_NOTARRIVED"), 0));
                            return;
                        }
                        if(3000 > Session.Character.Gold)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        Session.Character.Gold -= 3000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        MapCell pos = map.Map.GetRandomPosition();
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, map.MapInstanceId, pos.X, pos.Y);
                    }
                    break;

                //From Act4
                case 5002:
                    Session.ReceivePacket("$LeaveAct4");
                    break;

                case 5004:
                    if (npc != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, (short)(50 + ServerManager.RandomNumber(-3, 3)), (short)(41 + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                case 5011:
                    if (npc != null)
                    {
                        return;
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 170, (short)(119 + ServerManager.RandomNumber(-3, 3)), (short)(62 + ServerManager.RandomNumber(-3, 3)));
                    break;

                case 5012:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-3, 3)), (short)(tp.MapY + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                case 5014:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, (short)(tp.MapX + ServerManager.RandomNumber(-3, 3)), (short)(tp.MapY + ServerManager.RandomNumber(-3, 3)));
                    }
                    if (npc.NpcVNum == 1350)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, (short)(49 + ServerManager.RandomNumber(-3, 3)), (short)(44 + ServerManager.RandomNumber(-3, 3)));
                    }
                    break;

                case 3010:
                    if (npc == null)
                    {
                        return;
                    }
                    List<ClientSession> sessions2 = new List<ClientSession>();
                    if (Session.Character.Group != null)
                    {
                        sessions2 = Session.Character.Group.Sessions.Where(s => s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && s.Character.MapId == 4400);
                    }
                    else
                    {
                        sessions2.Add(Session);
                    }
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }
                    if (Session.Character.IsSeal)
                    {
                        return;
                    }

                    if (Session.Character.Group != null
                               && Session.Character.Group?.GroupType != GroupType.Group)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("NOT_POSSIBLE"), 0));
                        return;
                    }
                    List<Tuple<MapInstance, byte>> maps2 = new List<Tuple<MapInstance, byte>>(); // Here
                    MapInstance map2 = null;
                    switch (packet.Type)
                    {
                        #region Leveling Instances
                        case 0:
                            if (Session.Character.Level < 15)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30001, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 16, 40);
                                }
                            }
                            break;

                        case 1:
                            if (Session.Character.Level < 35)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30002, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 16, 40);
                                }
                            }
                            break;

                        case 2:
                            if (Session.Character.Level < 55)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30003, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 14, 55);
                                }
                            }
                            break;

                        case 3:
                            if (Session.Character.Level < 70)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30004, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 4:
                            if (Session.Character.Level < 80)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30005, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 5, 20);
                                }
                            }
                            break;
                        #endregion

                        case 5:
                            if (Session.Character.Level < 70)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30010, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 6:
                            if (Session.Character.Level < 80)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30011, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 7:
                            if (Session.Character.Level < 90)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30012, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 8:
                            if (Session.Character.Level < 93)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30013, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 9:
                            if (Session.Character.HeroLevel < 10)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30014, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 10:
                            if (Session.Character.HeroLevel < 10)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30015, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 11:
                            if (Session.Character.HeroLevel < 10)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30016, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 12:
                            if (Session.Character.HeroLevel < 10)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30017, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 13:
                            if (Session.Character.Level < 70)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30020, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 14:
                            if (Session.Character.Level < 70)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30021, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;

                        case 15:
                            if (Session.Character.Level < 70)
                            {
                                return;
                            }
                            map2 = ServerManager.GenerateMapInstance(30022, MapInstanceType.BaseMapInstance, new InstanceBag());
                            maps2.Add(new Tuple<MapInstance, byte>(map2, 20));
                            if (map2 != null)
                            {
                                foreach (ClientSession s in sessions2)
                                {
                                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map2.MapInstanceId, 34, 64);
                                }
                            }
                            break;
                    }
                    break;

                case 9001:
                    if (npc == null)
                    {
                        return;
                    }
                    switch (packet.Type)
                    {
                        case 0:
                            #region SP1
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    Session.Character.GiftAdd(901, 1);
                                    break;

                                case ClassType.Archer:
                                    Session.Character.GiftAdd(903, 1);
                                    break;

                                case ClassType.Magician:
                                    Session.Character.GiftAdd(905, 1);
                                    break;
                            }
                            #endregion
                            break;

                        case 1:
                            #region SP2
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    Session.Character.GiftAdd(902, 1);
                                    break;

                                case ClassType.Archer:
                                    Session.Character.GiftAdd(904, 1);
                                    break;

                                case ClassType.Magician:
                                    Session.Character.GiftAdd(906, 1);
                                    break;
                            }
                            #endregion
                            break;

                        case 2:
                            #region SP3
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(909))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 20)
                                    {
                                        Session.Character.GiftAdd(909, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 20);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 20", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(911))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 20)
                                    {
                                        Session.Character.GiftAdd(911, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 20);
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(913))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 20)
                                    {
                                        Session.Character.GiftAdd(913, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 20);
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 3:
                            #region SP4
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(910))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 40)
                                    {
                                        Session.Character.GiftAdd(910, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 40);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 40", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(912))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 40)
                                    {
                                        Session.Character.GiftAdd(912, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 40);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 40", 11));
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(914))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 40)
                                    {
                                        Session.Character.GiftAdd(914, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 40);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 40", 11));
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 4:
                            #region SP5
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(4500))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 100)
                                    {
                                        Session.Character.GiftAdd(4500, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 100);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 100", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(4501))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 100)
                                    {
                                        Session.Character.GiftAdd(4501, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 100);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 100", 11));
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(4502))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 100)
                                    {
                                        Session.Character.GiftAdd(4502, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 100);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 100", 11));
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 5:
                            #region SP6
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(4497))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4497, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(4498))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4498, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(4499))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4499, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 6:
                            #region SP7
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(4493))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4493, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(4492))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4492, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(4491))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4491, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 7:
                            #region SP8
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    if (!Session.Character.Inventory.CanAddItem(4489))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4489, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Archer:
                                    if (!Session.Character.Inventory.CanAddItem(4488))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4488, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;

                                case ClassType.Magician:
                                    if (!Session.Character.Inventory.CanAddItem(4487))
                                    {
                                        Session.SendPacket($"info You dont have enough space in your Inventory!");
                                        return;
                                    }
                                    if (Session.Character.Inventory.CountItem(2417) >= 200)
                                    {
                                        Session.Character.GiftAdd(4487, 1);
                                        Session.Character.Inventory.RemoveItemAmount(2417, 200);
                                    }
                                    else
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                                    }
                                    break;
                            }
                            #endregion
                            break;

                        case 8:
                            #region SP9
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:

                                    break;

                                case ClassType.Archer:

                                    break;

                                case ClassType.Magician:

                                    break;
                            }
                            #endregion
                            break;
                    }
                    break;


                case 9003:
                    if (npc == null)
                    {
                        return;
                    }
                    #region Archer
                    switch (packet.Type)
                    {
                        case 0:
                            Session.Character.AddQuest(2008, false);
                            break;

                        case 1:
                            Session.Character.AddQuest(2014, false);
                            break;

                        case 2:
                            if (!Session.Character.Inventory.CanAddItem(911))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 20)
                            {
                                Session.Character.GiftAdd(911, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 20);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 20", 11));
                            }
                            break;

                        case 3:
                            if (!Session.Character.Inventory.CanAddItem(912))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 40)
                            {
                                Session.Character.GiftAdd(912, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 40);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 40", 11));
                            }
                            break;

                        case 4:
                            if (!Session.Character.Inventory.CanAddItem(4501))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 100)
                            {
                                Session.Character.GiftAdd(4501, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 100);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 100", 11));
                            }
                            break;

                        case 5:
                            if (!Session.Character.Inventory.CanAddItem(4498))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4498, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;

                        case 6:
                            if (!Session.Character.Inventory.CanAddItem(4492))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4492, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;

                        case 7:
                            if (!Session.Character.Inventory.CanAddItem(4488))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4488, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;
                    }
                    #endregion
                    break;

                case 9004:
                    if (npc == null)
                    {
                        return;
                    }
                    #region Magician
                    switch (packet.Type)
                    {
                        case 0:
                            Session.Character.AddQuest(2008, false);
                            break;

                        case 1:
                            Session.Character.AddQuest(2014, false);
                            break;

                        case 2:
                            if (!Session.Character.Inventory.CanAddItem(913))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 20)
                            {
                                Session.Character.GiftAdd(913, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 20);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 20", 11));
                            }
                            break;

                        case 3:
                            if (!Session.Character.Inventory.CanAddItem(914))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 40)
                            {
                                Session.Character.GiftAdd(914, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 40);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 40", 11));
                            }
                            break;

                        case 4:
                            if (!Session.Character.Inventory.CanAddItem(4502))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 100)
                            {
                                Session.Character.GiftAdd(4502, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 100);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 100", 11));
                            }
                            break;

                        case 5:
                            if (!Session.Character.Inventory.CanAddItem(4499))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4499, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;

                        case 6:
                            if (!Session.Character.Inventory.CanAddItem(4491))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4491, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;

                        case 7:
                            if (!Session.Character.Inventory.CanAddItem(4487))
                            {
                                Session.SendPacket($"info You dont have enough space in your Inventory!");
                                return;
                            }
                            if (Session.Character.Inventory.CountItem(2417) >= 200)
                            {
                                Session.Character.GiftAdd(4487, 1);
                                Session.Character.Inventory.RemoveItemAmount(2417, 200);
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You don't have enough Specialist Shards! Amount required: 200", 11));
                            }
                            break;
                    }
                    #endregion
                    break;



                case 9007:
                    if (npc == null)
                    {
                        return;
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 170, (short)(119 + ServerManager.RandomNumber(-3, 3)), (short)(62 + ServerManager.RandomNumber(-3, 3)));
                    break;

                case 9010:
                    if (npc == null)
                    {
                        return;
                    }
                    if (Session.Character.Class == ClassType.Swordsman)
                    {
                        Session.Character.AddSkill(222);
                        Session.Character.AddSkill(223);
                        Session.Character.AddSkill(224);
                        Session.Character.AddSkill(225);
                        Session.Character.AddSkill(226);
                        Session.Character.AddSkill(227);
                        Session.Character.AddSkill(228);
                        Session.Character.AddSkill(229);
                        Session.Character.AddSkill(230);
                        Session.Character.AddSkill(231);
                        Session.Character.AddSkill(232);
                        Session.Character.AddSkill(233);
                        Session.Character.AddSkill(234);
                        Session.SendPacket(Session.Character.GenerateSki());
                    }
                    else
                    {
                        Session.SendPacket("info You cannot learn the Skills of a Swordsman");
                    }
                    break;

                case 9011:
                    if (npc == null)
                    {
                        return;
                    }
                    if (Session.Character.Class == ClassType.Archer)
                    {
                        Session.Character.AddSkill(242);
                        Session.Character.AddSkill(243);
                        Session.Character.AddSkill(244);
                        Session.Character.AddSkill(245);
                        Session.Character.AddSkill(246);
                        Session.Character.AddSkill(247);
                        Session.Character.AddSkill(248);
                        Session.Character.AddSkill(249);
                        Session.Character.AddSkill(250);
                        Session.Character.AddSkill(251);
                        Session.Character.AddSkill(252);
                        Session.Character.AddSkill(253);
                        Session.Character.AddSkill(254);
                        Session.Character.AddSkill(255);
                        Session.Character.AddSkill(256);
                        Session.SendPacket(Session.Character.GenerateSki());
                    }
                    else
                    {
                        Session.SendPacket("info You cannot learn the Skills of a Archer");
                    }
                    break;

                case 9012:
                    if (npc == null)
                    {
                        return;
                    }
                    if (Session.Character.Class == ClassType.Magician)
                    {
                        Session.Character.AddSkill(262);
                        Session.Character.AddSkill(263);
                        Session.Character.AddSkill(264);
                        Session.Character.AddSkill(265);
                        Session.Character.AddSkill(266);
                        Session.Character.AddSkill(267);
                        Session.Character.AddSkill(268);
                        Session.Character.AddSkill(269);
                        Session.Character.AddSkill(270);
                        Session.Character.AddSkill(271);
                        Session.Character.AddSkill(272);
                        Session.Character.AddSkill(273);
                        Session.Character.AddSkill(274);
                        Session.Character.AddSkill(275);
                        Session.Character.AddSkill(276);
                        Session.Character.AddSkill(277);
                        Session.SendPacket(Session.Character.GenerateSki());
                    }
                    else
                    {
                        Session.SendPacket("info You cannot learn the Skills of a Magician");
                    }
                    break;


                case 150:
                    if (npc == null)
                    {
                        return;
                    }
                    switch (packet.Type)
                    {
                        #region Family
                        case 0:
                            if (npc != null)
                            {
                                if (Session.Character.Family != null)
                                {
                                    if (Session.Character.Family.LandOfDeath != null && ServerManager.Instance.StartedEvents.Contains(EventType.LOD) && npc.Effect != 0)
                                    {
                                        if (Session.Character.Level >= 55)
                                        {
                                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.Character.Family.LandOfDeath.MapInstanceId, (short)(153 + ServerManager.RandomNumber(-3, 2)), (short)(145 + ServerManager.RandomNumber(-3, 3)));
                                        }
                                        else
                                        {
                                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOD_REQUIERE_LVL"), 0));
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOD_CLOSED"), 0));
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NEED_FAMILY"), 0));
                                }
                            }
                            break;
                        #endregion

                        #region Group
                        case 1:
                            Session.SendPacket("info This Feature has been disabled for now");
                            break;
                        #endregion

                        #region Solo
                        case 2:
                            if (Session.Character.Level < 50)
                            {
                                Session.SendPacket("msg 4 Your Level is not high enough!");
                                return;
                            }
                            MapInstance mapLod2 = null;
                            mapLod2 = ServerManager.GenerateMapInstance(150, MapInstanceType.CustomInstance, new InstanceBag());
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapLod2.MapInstanceId, 150, 150);
                            break;
                        #endregion

                        #region Public
                        case 3:
                            if (Session.Character.Level < 49)
                            {
                                Session.SendPacket("msg 4 Your Level is not high enough!");
                                return;
                            }
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 150, 150, 150);
                            break;
                            #endregion
                    }
                    break;

                case 384:
                    if (npc == null)
                    {
                        return;
                    }
                    if (Session.Character.HeroLevel >= 30)
                    {
                        switch (packet.Type)
                        {
                            case 2700:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2700, 58, 82);
                                break;

                            case 1:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 1, 80, 116);
                                break;

                            case 145:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, 13, 110);
                                break;

                            case 20:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 20, 9, 92);
                                break;
                        }
                    }
                    else
                    {
                        Session.SendPacket("msg 4 Your Herolevel is not high enough!");
                    }
                    break;

                //Daily Quest
                //case 3006:
                //    if (npc == null)
                //    {
                //        return;
                //    }
                //    Session.Character.AddQuest(packet.Type, false);
                //    break;

                //Mystery Box
                case 12000:
                    switch (packet.Type)
                    {
                        case 0:
                            MysteryBoxExtension.GenerateMysteryBox(Session, packet);
                            break;

                        case 1:
                            MysteryBoxExtension.GenerateMysteryBoxLooped(Session, packet);
                            break;

                        case 2:
                            MysteryBoxExtension.GenerateMysteryBoxLooped(Session, packet);
                            break;
                    }
                    break;

                //Mystery Box Reward List
                case 12001:
                    MysteryBoxConfigrationExtension.GenerateRewardList(Session);
                    break;

                //Primal Quest
                case 12002:
                    switch (packet.Type)
                    {
                        //Character Quest
                        case 0:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 1);
                            break;

                        case 1:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 2);
                            break;

                        case 2:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 3);
                            break;

                        case 3:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 4);
                            break;

                        case 4:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 5);
                            break;

                        case 5:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 6);
                            break;

                        case 6:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 7);
                            break;

                        case 7:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 8);
                            break;

                        case 8:
                            PrimalQuestExtension.GenerateCharacterQuest(Session, 9);
                            break;

                        //Raid Quest
                        case 9:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 1);
                            break;

                        case 10:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 2);
                            break;

                        case 11:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 3);
                            break;

                        case 12:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 4);
                            break;

                        case 13:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 5);
                            break;

                        case 14:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 6);
                            break;

                        case 15:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 7);
                            break;

                        case 16:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 8);
                            break;

                        case 17:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 9);
                            break;

                        case 18:
                            PrimalQuestExtension.GenerateRaidQuest(Session, 10);
                            break;

                            #endregion
                    }
                    break;

                //Lottery System
                case 12003:
                    switch (packet.Type)
                    {
                        case 0:
                            LotteryService.GenerateLotteryAsync(Session);
                            break;

                        case 1:
                            LotteryService.GenerateLotteryInfoAsync(Session);
                            break;

                        case 2:
                            LotteryService.GenerateLotteryChancesAsync(Session);
                            break;
                    }
                    break;

                case 12004:
                    switch (packet.Type)
                    {
                        case 0:
                            BuffBookExtension.Buy(Session);
                            break;

                        case 1:
                            BuffBookExtension.ChargeBy10(Session);
                            break;

                        case 2:
                            BuffBookExtension.ChargeBy50(Session);
                            break;

                        case 3:
                            BuffBookExtension.ChargeFull(Session);
                            break;
                    }
                    break;

                case 12005:
                    switch (packet.Type)
                    {
                        //NosVille
                        case 0:
                             TeleportationExtension.Teleport(Session, 10000, 1, 80, 116, 1);
                            break;
                        //Port Alveus
                        case 1:
                             TeleportationExtension.Teleport(Session, 10000, 145, 59, 110, 1);
                            break;
                        //Land of Death
                        case 2:
                             TeleportationExtension.Teleport(Session, 25000, 98, 11, 29, 1);
                            break;
                        //Raid Lobby
                        case 3:
                             MessageExtension.SendInfo(Session, "This Point doesnt work yet.");
                            // TeleportationExtension.Teleport(Session, 10000, 20052, 38, 20, 1);
                            break;
                        //Fernon Outpost
                        case 4:
                             TeleportationExtension.Teleport(Session, 10000, 5, 100, 217, 1);
                            break;
                        //Western Krem
                        case 5:
                             TeleportationExtension.Teleport(Session, 25000, 20, 9, 92, 1);
                            break;
                        //Desert Eagle City
                        case 6:
                             TeleportationExtension.Teleport(Session, 50000, 189, 57, 160, 1);
                            break;
                        //Volcano Gate
                        case 7:
                             TeleportationExtension.Teleport(Session, 50000, 179, 128, 121, 1);
                            break;
                        //Cylloan
                        case 8:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 228, 63, 108, 85, 1);
                            break;
                        //Hell's Gate 4
                        case 9:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 236, 104, 321, 85, 1);
                            break;
                        //Heaven's Gate 4
                        case 10:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 232, 112, 322, 85, 1);
                            break;
                        //Ancelloan's Will 1
                        case 11:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 241, 71, 166, 85, 1);
                            break;
                        //Olorun Village
                        case 12:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 2628, 72, 70, 85, 10);
                            break;
                        //Dragonveil
                        case 13:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 2700, 57, 73, 85, 30);
                            break;

                        //Undercity
                        case 14:
                             TeleportationExtension.TeleportHeroic(Session, 50000, 280, 85, 86, 99, 60);
                            break;

                        default:
                             MessageExtension.SendBubble(Session, "This Teleport-Point has been De-Activated");
                            break;
                    }
                    break;

                case 12006:
                    switch (packet.Type)
                    {
                        case 0:
                            Session.Character.Compliment = 10;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 1:
                            Session.Character.Compliment = 20;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 2:
                            Session.Character.Compliment = 30;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 3:
                            Session.Character.Compliment = 40;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 4:
                            Session.Character.Compliment = 50;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 5:
                            Session.Character.Compliment = 60;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 6:
                            Session.Character.Compliment = 70;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 7:
                            Session.Character.Compliment = 80;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 8:
                            Session.Character.Compliment = 90;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;

                        case 9:
                            Session.Character.Compliment = 100;
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                       Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                       true);
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            break;
                    }
                    break;

                case 12007:
                    {
                        SpecialistExchangeExtension.Exchange(Session, packet);
                    }
                    break;

                case 12008:
                    switch (packet.Type)
                    {
                        case 0:
                            CharacterConfigurationExtension.Set(Session, CharacterConfigurationType.AutoLoot);
                            break;

                        case 1:
                            CharacterConfigurationExtension.Set(Session, CharacterConfigurationType.SafeBet);
                            break;

                        case 2:
                            break;

                        case 3:
                            break;

                        case 4:
                            break;

                        case 5:
                            break;

                        case 6:
                            break;
                    }
                    break;

                case 9000:
                    MagicianExtension.UpgradeFairy(Session);
                    break;

                case 25000:
                    Session.Character.AddQuest(25000);
                    break;

                case 25001:
                    Session.Character.AddQuest(25001);
                    break;

                case 25002:
                    Session.Character.AddQuest(25002);
                    break;

                case 25003:
                    Session.Character.AddQuest(25003);
                    break;

                case 25004:
                    Session.Character.AddQuest(25004);
                    break;

                case 25005:
                    Session.Character.AddQuest(25005);
                    break;


                default:
                    {
                        Session.SendPacket($"info NRun {packet.Runner} wasn't found. Please report this to the Admin-Team!");
                        ////await //LOGGER($"[NRUN] Missing implementation: Runner: {packet.Runner} | Type: {packet.Type}");
                    }
                    break;
            }
        }

    }
}