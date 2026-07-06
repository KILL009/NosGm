using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G400 : IGuriHandler
    {
        public long GuriEffectId => 400;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 400)
            {
                if (!Session.HasCurrentMapInstance)
                {
                    return;
                }
                    
                var mapNpcId = e.Argument;

                var npc = Session.CurrentMapInstance.Npcs.Find(n => n.MapNpcId.Equals(mapNpcId));

                if (npc != null && !npc.IsOut)
                {
                    var mapobject = ServerManager.GetNpcMonster(npc.NpcVNum);

                    var rateDrop = GameConfiguration.DropRate;
                    var delay = (int)Math.Round(
                        (3 + mapobject.RespawnTime / 1000d) * Session.Character.TimesUsed);
                    delay = delay > 11 ? 8 : delay;
                    if (npc.NpcVNum == 1346 || npc.NpcVNum == 2350 || npc.NpcVNum == 2556 || npc.NpcVNum == 2060 || npc.NpcVNum == 881 || npc.NpcVNum == 424)
                    {
                        delay = 0;
                    }
                    if (Session.Character.LastMapObject.AddSeconds(delay) < DateTime.Now)
                    {
                        if (mapobject.Drops.Any(s => s.MonsterVNum != null) && mapobject.VNumRequired > 10
                                                                            && Session.Character.Inventory.CountItem(mapobject.VNumRequired)
                                                                            < mapobject.AmountRequired)
                        {
                            if (ServerManager.GetItem(mapobject.VNumRequired) is Item requiredItem)
                            {
                                Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                                string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), mapobject.AmountRequired, requiredItem.Name), 0));
                            }

                            return;
                        }

                        var random = new Random();
                        var randomAmount = ServerManager.RandomNumber() * random.NextDouble();
                        var drops = mapobject.Drops.Where(s => s.MonsterVNum == npc.NpcVNum).ToList();
                        if (drops?.Count > 0)
                        {
                            var count = 0;
                            var probabilities = drops.Sum(s => s.DropChance);
                            var rnd = ServerManager.RandomNumber(0, probabilities);
                            var currentrnd = 0;
                            var firstDrop = mapobject.Drops.Find(s => s.MonsterVNum == npc.NpcVNum);

                            if (npc.NpcVNum == 2004 /* Ice Flower */ && firstDrop != null)
                            {
                                var newInv = Session.Character.Inventory.AddNewToInventory(firstDrop.ItemVNum, (short)firstDrop.Amount).FirstOrDefault();

                                if (newInv != null)
                                {
                                    Session.Character.IncrementQuests(QuestType.Collect1, firstDrop.ItemVNum);
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"), $"{newInv.Item.Name} x {firstDrop.Amount}"), 0));
                                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"), $"{newInv.Item.Name} x {firstDrop.Amount}"), 11));
                                }
                                else
                                {
                                    Session.Character.GiftAdd(firstDrop.ItemVNum, (short)firstDrop.Amount);
                                }

                                Session.CurrentMapInstance.Broadcast(npc.GenerateOut());
                                npc.MapInstance.RemoveNpc(npc);

                                return;
                            }
                            else if (randomAmount * 1000 <= probabilities)
                            {
                                foreach (var drop in drops.OrderBy(s => ServerManager.RandomNumber()))
                                {
                                    var vnum = drop.ItemVNum;
                                    var amount = (short)drop.Amount;
                                    var dropChance = drop.DropChance;
                                    currentrnd += drop.DropChance;
                                    if (currentrnd >= rnd)
                                    {
                                        var newInv = Session.Character.Inventory.AddNewToInventory(vnum, amount)
                                                            .FirstOrDefault();
                                        if (newInv != null)
                                        {
                                            if (dropChance != 100000)
                                            {
                                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                    string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"),
                                                        $"{newInv.Item.Name} x {amount}"), 0));
                                            }
                                            Session.SendPacket(Session.Character.GenerateSay(
                                                string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"),
                                                    $"{newInv.Item.Name} x {amount}"), 11));
                                            Session.Character.IncrementQuests(QuestType.Collect1, vnum);
                                        }
                                        else
                                        {
                                            Session.Character.GiftAdd(vnum, amount);
                                        }
                                        count++;
                                        if (dropChance != 100000)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                            if (count > 0)
                            {
                                Session.Character.LastMapObject = DateTime.Now;
                                Session.Character.TimesUsed++;
                                if (Session.Character.TimesUsed >= 4)
                                {
                                    Session.Character.TimesUsed = 0;
                                }

                                if (mapobject.VNumRequired > 10)
                                {
                                    Session.Character.Inventory.RemoveItemAmount(npc.Npc.VNumRequired, npc.Npc.AmountRequired);
                                }

                                if (npc.NpcVNum == 1346 || npc.NpcVNum == 2350 || npc.NpcVNum == 2556 || npc.NpcVNum == 2060 || npc.NpcVNum == 881 || npc.NpcVNum == 424)
                                {
                                    npc.SetDeathStatement();
                                    npc.RunDeathEvent();
                                    Session.Character.MapInstance.Broadcast(npc.GenerateOut());
                                }
                            }
                            else
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("TRY_FAILED"), 0));
                            }
                        }
                        else if (Session.CurrentMapInstance.Npcs.Where(s => s.Npc.Race == 8 && s.Npc.RaceType == 5 && s.MapNpcId != npc.MapNpcId) is IEnumerable<MapNpc> mapTeleportNpcs)
                        {
                            if (npc.Npc.VNumRequired > 0 && npc.Npc.AmountRequired > 0)
                            {
                                if (Session.Character.Inventory.CountItem(npc.Npc.VNumRequired) >= npc.Npc.AmountRequired)
                                {
                                    if (npc.Npc.AmountRequired > 1)
                                    {
                                        Session.Character.Inventory.RemoveItemAmount(npc.Npc.VNumRequired, npc.Npc.AmountRequired);
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"), 0));
                                    return;
                                }
                            }
                            if (DAOFactory.TeleporterDAO.LoadFromNpc(npc.MapNpcId).FirstOrDefault() is TeleporterDTO teleport)
                            {
                                Session.Character.PositionX = teleport.MapX;
                                Session.Character.PositionY = teleport.MapY;
                                Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTp());
                                foreach (var mate in Session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive))
                                {
                                    mate.PositionX = teleport.MapX;
                                    mate.PositionY = teleport.MapY;
                                    Session.CurrentMapInstance.Broadcast(mate.GenerateTp());
                                }
                            }
                            else
                            {
                                MapNpc nearestTeleportNpc = null;
                                foreach (var teleportNpc in mapTeleportNpcs)
                                {
                                    if (nearestTeleportNpc == null)
                                    {
                                        nearestTeleportNpc = teleportNpc;
                                    }
                                    else if (
                                        Map.GetDistance(
                                            new MapCell { X = npc.MapX, Y = npc.MapY },
                                            new MapCell { X = teleportNpc.MapX, Y = teleportNpc.MapY })
                                        <
                                        Map.GetDistance(
                                            new MapCell { X = npc.MapX, Y = npc.MapY },
                                            new MapCell { X = nearestTeleportNpc.MapX, Y = nearestTeleportNpc.MapY }))
                                    {
                                        nearestTeleportNpc = teleportNpc;
                                    }
                                }
                                if (nearestTeleportNpc != null)
                                {
                                    Session.Character.PositionX = nearestTeleportNpc.MapX;
                                    Session.Character.PositionY = nearestTeleportNpc.MapY;
                                    Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTp());
                                    foreach (var mate in Session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive))
                                    {
                                        mate.PositionX = nearestTeleportNpc.MapX;
                                        mate.PositionY = nearestTeleportNpc.MapY;
                                        Session.CurrentMapInstance.Broadcast(mate.GenerateTp());
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("TRY_FAILED_WAIT"),
                                (int)(Session.Character.LastMapObject.AddSeconds(delay) - DateTime.Now)
                                .TotalSeconds), 0));
                    }
                }
            }
        }
    }
}