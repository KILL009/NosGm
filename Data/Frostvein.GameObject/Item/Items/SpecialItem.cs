using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Items;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Frostvein.GameObject.Extension.Message;
using System.Threading;
using Frostvein.GameObject.Service;
using Frostvein.GameObject.ItemThread;

using Frostvein.GameObject.Extension.Inventory;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Frostvein.GameObject
{
    public class SpecialItem : Item
    {
        private Random rngProvider = new Random((int)(DateTime.UtcNow.Ticks * Environment.TickCount));

        #region Instantiation

        public SpecialItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override async void Use(ClientSession session, ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            try
            {
                var itemDesign = inv.Design;

                if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
                {
                    session.SendPacket("msg 4 You cannot do that here");
                    return;
                }

                switch (VNum)
                {
                    case 13200:
                        VNum13200.Execute(session);
                        break;

                    case 14005:
                        if (EffectValue == 1 && byte.TryParse(packetsplit[9], out var islot))
                        {
                            var randombox = session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Main);

                            if (randombox != null)
                            {
                                string Items = "";
                                int i = 0;
                                foreach (RaidboxDTO item in DAOFactory.RaidboxDAO.LoadByItemVNumAndDesign(randombox.ItemVNum, randombox.Design))
                                {
                                    Item ite = ServerManager.GetItem(item.ItemGeneratedVNum);
                                    Items += $" {i++}.{item.ItemGeneratedVNum}.2.{item.ItemGeneratedAmount}.0.0";
                                }
                                session.SendPacket($"f_stash_all 0 " + Items);
                            }
                        }
                        else
                        {
                            
                        }
                        break;

                    case 14006:
                        if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                        {
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5002 });
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.PositionX, session.Character.PositionY);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            MessageExtension.SendBubble(session, "This Item can only be used inside of the Glacerus Raid");
                        }
                        break;

                    case 15000:
                        session.Character.Compliment = 50;
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 14007:
                        if (session.Character.HasBuff(5003)) return;
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5003 });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 14008:
                        if (session.Character.HasBuff(5004)) return;
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5004 });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 14009:
                        if (session.Character.HasBuff(5005)) return;
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5005 });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 14003:
                        BuffBookExtension.ApplyBuffs(session);
                        break;

                    case 1458:
                        await VNum1458.Execute(session);
                        break;

                    case 5119:
                        await VNum5119.Execute(session, inv);
                        break;

                    case 9071:
                        await VNum9071.Execute(session);
                        break;

                    case 1907:
                        break;

                    case 9283:
                        if (session.Character.HasPremiumBattlePass)
                        {
                            session.SendPacket("info That wont work");
                            return;
                        }
                        await VNum9283.Execute(session);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9591:
                        WingChangerExtension.GenerateChange(session);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 11008:
                        await DailyRewardExtension.GenerateReward(session);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 11009:
                        await DialogExtension.GenerateDialog(session, 12005);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 11010:
                        await DialogExtension.GenerateDialog(session, 12005);
                        break;

                    case 11011:
                        if (session.Character.MapId == 150)
                        {
                            session.SendPacket("info You cannot do that here");
                            return;
                        }
                        await VNum11011.Execute(session);
                        break;

                    case 13006:
                        await VNum13006.Execute(session);
                        break;

                    case 13007:
                        await VNum13007.Execute(session);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 13008:
                        WingChangerExtension.GenerateChange(session);
                        break;

                    //Attack, Defense and HP/MP Potion
                    case 14010:
                        session.Character.AddBuff(new Buff(1, session.Character.Level), session.Character.BattleEntity);
                        session.Character.AddBuff(new Buff(2, session.Character.Level), session.Character.BattleEntity);
                        session.Character.AddBuff(new Buff(3, session.Character.Level), session.Character.BattleEntity);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    //Rocketeer Set
                    case 9577:
                        session.Character.GiftAdd(4679, 1);
                        session.Character.GiftAdd(4681, 1);
                        session.Character.GiftAdd(4683, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9846:
                        await VNum9846.Execute(session, inv );
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id); //<- inv = ItemInstance = Clicked Item | Has to be removed via ID since it's unique
                        //When you remove an Item, just hand out the inv.Id since it takes the GUID from ItemInstance.
                        //An item is never really removed using VNUM, since it could easily cause issues when the Database takes a bit too long to react
                        //Therefore, you create a GUID (a ultra unique ID, can be seen in the Database. Looks like LKJPHASDLK-98213JD-2938MDAS. Therefore you only remove the 
                        //current Inventory (inv) that actually is the ItemInstance. For your understanding, the Database includes the Character's Inventory, but it's called 
                        //ItemInstance - ItemInstance is the current Item that can be loaded or checked when using an Item. And since you are using this specific Item (9846)
                        //It will remove the Item that you are currently clicking on.
                        //If you want to remove an Item except the Item that you are using, you can use RemoveItemAmount(VNUM, AMOUNT)
                        break;

                    case 5989:
                        await VNum5989.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9833:
                        await VNum9833.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9834:
                        await VNum9834.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9835:
                        await VNum9835.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9836:
                        await VNum9836.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9837:
                        await VNum9837.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9838:
                        await VNum9838.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9840:
                        await VNum9840.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9842:
                        await VNum9842.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9844:
                        await VNum9844.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9845:
                        await VNum9845.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 5299:
                        //await VNum5299.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9871:
                        await VNum9871.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9872:
                        await VNum9872.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9848:
                        await VNum9848.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9849:
                        await VNum9849.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9850:
                        await VNum9850.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    case 9851:
                        await VNum9851.Execute(session, inv);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;



                    //Fernon Raidbox
                    //case 9846:
                    //    await VNum9846.Execute(session, inv);
                    //    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    //    break; 

                    //Sporty Weapon Skins
                    case 13011:
                        int rnd1 = ServerManager.RandomNumber(0, 100);
                        if (session.Character.PityCount == 100)
                        {
                            switch (session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    session.Character.GiftAdd(4271, 1);
                                    break;
                                case ClassType.Archer:
                                    session.Character.GiftAdd(4273, 1);
                                    break;
                                case ClassType.Magician:
                                    session.Character.GiftAdd(4275, 1);
                                    break;
                            }
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            MessageExtension.SendYellow(session, $"You received the Main Reward and your Pity has been resetted");
                            session.Character.PityCount = 0;
                            return;
                        }
                        if (rnd1 < 3)
                        {
                            switch (session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    session.Character.GiftAdd(4271, 1);
                                    break;
                                case ClassType.Archer:
                                    session.Character.GiftAdd(4273, 1);
                                    break;
                                case ClassType.Magician:
                                    session.Character.GiftAdd(4275, 1);
                                    break;
                            }
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.PityCount = 0;
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                        }
                        else
                        {
                            short[] vnums2 = null;
                            vnums2 = new short[] { 2160, 1119, 284, 1285, 1904, 1296, 1945 };
                            byte[] counts2 = { 40, 1, 1, 16, 1, 10, 14 };
                            int item2 = ServerManager.RandomNumber(0, 7);
                            session.Character.GiftAdd(vnums2[item2], counts2[item2]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.PityCount++;
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                        }
                        break;

                    //Otter Random Box
                    case 13012:
                        try
                        {
                            int rnd2 = ServerManager.RandomNumber(0, 100);
                            if (session.Character.PityCount == 100)
                            {
                                session.Character.GiftAdd(4464, 1);
                                MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                                MessageExtension.SendYellow(session, $"You received the Main Reward and your Pity has been resetted");
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.Character.PityCount = 0;
                                return;
                            }
                            if (rnd2 < 3)
                            {
                                session.Character.GiftAdd(4464, 1);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.Character.PityCount = 0;
                                MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                            }
                            else
                            {
                                short[] vnums2 = null;
                                vnums2 = new short[] { 2160, 1119, 284, 1285, 1904, 1296, 1945 };
                                byte[] counts2 = { 40, 1, 1, 16, 1, 10, 14 };
                                int item2 = ServerManager.RandomNumber(0, 7);
                                session.Character.GiftAdd(vnums2[item2], counts2[item2]);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.Character.PityCount++;
                                MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                            }
                        }
                        catch (Exception e)
                        {
                            MessageExtension.SendRed(session, "An Error occured, please report this  to an Admin");
                            Logger.Warn(e.ToString());
                        }
                        break;

                    //Rocketeer Random Box
                    case 13013:

                        int rnd3 = ServerManager.RandomNumber(0, 100);
                        if (session.Character.PityCount == 100)
                        {
                            session.Character.GiftAdd(9577, 1);
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                            MessageExtension.SendYellow(session, $"You received the Main Reward and your Pity has been resetted");
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.PityCount = 0;
                            return;
                        }
                        if (rnd3 < 3)
                        {
                            session.Character.GiftAdd(9577, 1);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.PityCount = 0;
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                        }
                        else
                        {
                            short[] vnums2 = null;
                            vnums2 = new short[] { 2160, 1119, 284, 1285, 1904, 1296, 1945 };
                            byte[] counts2 = { 40, 1, 1, 16, 1, 10, 14 };
                            int item2 = ServerManager.RandomNumber(0, 7);
                            session.Character.GiftAdd(vnums2[item2], counts2[item2]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.PityCount++;
                            MessageExtension.SendYellow(session, $"Pity Count: {session.Character.PityCount}/100");
                        }
                        break;

                    default:
                        Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType(), VNum,
                           Effect, EffectValue));
                        break;
                }

                if (session.Character.IsVehicled && Effect != 1000)
                {
                    if (VNum == 5119 || VNum == 9071) // Speed Booster
                    {
                        if (!session.Character.Buff.Any(s => s.Card.CardId == 336))
                        {
                            session.Character.VehicleItem.BCards.ForEach(s =>
                                s.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                            session.CurrentMapInstance.Broadcast($"eff 1 {session.Character.CharacterId} 885");
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(
                            session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                    }

                    return;
                }

                if (VNum == 9618) //Family Level UP 2
                {
                    if (session.Character.Family == null)
                    {
                        return;
                    }

                    var dto = DAOFactory.FamilyDAO.LoadById(session.Character.Family.FamilyId);

                    if (dto == null)
                    {
                        return;
                    }

                    dto.FamilyLevel = 2;
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = session.Character.Family.FamilyId,
                        SourceCharacterId = session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg($"{session.Character.Name} increased the family level to 2!", 0),
                        Type = MessageType.Family
                    });
                    DAOFactory.FamilyDAO.InsertOrUpdate(ref dto);

                    ServerManager.Instance.FamilyRefresh(session.Character.Family.FamilyId);
                    var sessionsInFamily = session.CurrentMapInstance.Sessions.ToList().Where(s => s.Character.Family?.FamilyId == session.Character.Family.FamilyId);

                    foreach (var Session in sessionsInFamily)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateGidx());
                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                }
                if (VNum == 9619) //Family Level UP 3
                {
                    if (session.Character.Family == null)
                    {
                        return;
                    }

                    var dto = DAOFactory.FamilyDAO.LoadById(session.Character.Family.FamilyId);

                    if (dto == null)
                    {
                        return;
                    }

                    dto.FamilyLevel = 3;
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = session.Character.Family.FamilyId,
                        SourceCharacterId = session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg($"{session.Character.Name} increased the family level to 3!", 0),
                        Type = MessageType.Family
                    });
                    DAOFactory.FamilyDAO.InsertOrUpdate(ref dto);

                    ServerManager.Instance.FamilyRefresh(session.Character.Family.FamilyId);
                    var sessionsInFamily = session.CurrentMapInstance.Sessions.ToList().Where(s => s.Character.Family?.FamilyId == session.Character.Family.FamilyId);

                    foreach (var Session in sessionsInFamily)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateGidx());
                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                }
                if (VNum == 9620) //Family Level UP 4
                {
                    if (session.Character.Family == null)
                    {
                        return;
                    }

                    var dto = DAOFactory.FamilyDAO.LoadById(session.Character.Family.FamilyId);

                    if (dto == null)
                    {
                        return;
                    }

                    dto.FamilyLevel = 4;
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = session.Character.Family.FamilyId,
                        SourceCharacterId = session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg($"{session.Character.Name} increased the family level to 4!", 0),
                        Type = MessageType.Family
                    });
                    DAOFactory.FamilyDAO.InsertOrUpdate(ref dto);

                    ServerManager.Instance.FamilyRefresh(session.Character.Family.FamilyId);
                    var sessionsInFamily = session.CurrentMapInstance.Sessions.ToList().Where(s => s.Character.Family?.FamilyId == session.Character.Family.FamilyId);

                    foreach (var Session in sessionsInFamily)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateGidx());
                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                }
                if (VNum == 9621) //Family Level UP 5
                {
                    if (session.Character.Family == null)
                    {
                        return;
                    }

                    var dto = DAOFactory.FamilyDAO.LoadById(session.Character.Family.FamilyId);

                    if (dto == null)
                    {
                        return;
                    }

                    dto.FamilyLevel = 5;
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = session.Character.Family.FamilyId,
                        SourceCharacterId = session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg($"{session.Character.Name} increased the family level to 5!", 0),
                        Type = MessageType.Family
                    });
                    DAOFactory.FamilyDAO.InsertOrUpdate(ref dto);

                    ServerManager.Instance.FamilyRefresh(session.Character.Family.FamilyId);
                    var sessionsInFamily = session.CurrentMapInstance.Sessions.ToList().Where(s => s.Character.Family?.FamilyId == session.Character.Family.FamilyId);

                    foreach (var Session in sessionsInFamily)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateGidx());
                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                }
                if (VNum == 7159) //Perfection Reset
                {
                    if (session.Character.UseSp == true)
                    {
                        ItemInstance specialistInstance = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                        if (specialistInstance != null)
                        {
                            specialistInstance.SpDamage = 0;
                            specialistInstance.SpDark = 0;
                            specialistInstance.SpDefence = 0;
                            specialistInstance.SpElement = 0;
                            specialistInstance.SpFire = 0;
                            specialistInstance.SpHP = 0;
                            specialistInstance.SpLight = 0;
                            specialistInstance.SpStoneUpgrade = 0;
                            specialistInstance.SpWater = 0;
                            session.Character.Inventory.RemoveItemAmount(7159, 1);
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RESET_PERFECTION"), 12));
                        }
                        else
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TRANSFORMATION_NEEDED"), 12));
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TRANSFORMATION_NEEDED"), 12));
                    }
                }

                if (VNum == 7341) // Arena Winner
                {
                    session.Character.ArenaWinner = session.Character.ArenaWinner == 0 ? 1 : 0;
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                    session.SendPacket(session.Character.GenerateSay("You can use this item to on/off the Arena Wings", 10));
                    return;
                }

                if (VNum == 7169) //Change Class Basic Swordman 
                {
                    if (session.Character.Class != ClassType.Swordsman)
                    {
                        if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                        {
                            session.Character.ChangeClass(ClassType.Swordsman, false);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                        }
                    }
                }

                if (VNum == 7170) //Change Class Basic Archer
                {
                    if (session.Character.Class != ClassType.Archer)
                    {
                        if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                        {
                            session.Character.ChangeClass(ClassType.Archer, false);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                        }
                    }
                }

                if (VNum == 7171) //Change Class Basic Magician
                {
                    if (session.Character.Class != ClassType.Magician)
                    {
                        if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                        {
                            session.Character.ChangeClass(ClassType.Magician, false);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                        }
                    }
                }
                if (VNum == 7164)  //SP Level Up 20
                {
                    ItemInstance sp = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Sp, InventoryType.Wear);

                    if (session.Character.UseSp == true)
                    {
                        if (sp != null)
                        {
                            if (sp.SpLevel < 20)
                            {
                                sp.SpLevel = 20;
                                sp.XP = 0;
                                session.SendPacket(session.Character.GenerateLev());
                                session.Character.LearnSPSkill();
                                session.SendPacket(session.Character.GenerateSki());
                                session.SendPackets(session.Character.GenerateQuicklist());
                                session.Character.SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_LEVELUP"), 0));
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 8), session.Character.PositionX, session.Character.PositionY);
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Your Special Card Level is already 20!"));
                            }
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo("Your need to be transformed for use that!"));
                    }
                }
                if (VNum == 7165) //SP Level Up 50
                {
                    ItemInstance sp = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Sp, InventoryType.Wear);

                    if (session.Character.UseSp == true)
                    {
                        if (sp != null)
                        {
                            if (sp.SpLevel < 50)
                            {
                                sp.SpLevel = 50;
                                sp.XP = 0;
                                session.SendPacket(session.Character.GenerateLev());
                                session.Character.LearnSPSkill();
                                session.SendPacket(session.Character.GenerateSki());
                                session.SendPackets(session.Character.GenerateQuicklist());
                                session.Character.SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_LEVELUP"), 0));
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 8), session.Character.PositionX, session.Character.PositionY);
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Your Special Card Level is already 50!"));
                            }
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo("Your need to be transformed for use that!"));

                    }
                }
                if (VNum == 7166) //SP Level Up 70
                {
                    ItemInstance sp = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Sp, InventoryType.Wear);

                    if (session.Character.UseSp == true)
                    {
                        if (sp != null)
                        {
                            if (sp.SpLevel < 70)
                            {
                                sp.SpLevel = 70;
                                sp.XP = 0;
                                session.SendPacket(session.Character.GenerateLev());
                                session.Character.LearnSPSkill();
                                session.SendPacket(session.Character.GenerateSki());
                                session.SendPackets(session.Character.GenerateQuicklist());
                                session.Character.SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_LEVELUP"), 0));
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 8), session.Character.PositionX, session.Character.PositionY);
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Your Special Card Level is already 70!"));
                            }
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo("Your need to be transformed for use that!"));

                    }
                }
                if (VNum == 7167) //SP Level Up 99
                {
                    ItemInstance sp = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Sp, InventoryType.Wear);

                    if (session.Character.UseSp == true)
                    {
                        if (sp != null)
                        {
                            if (sp.SpLevel < 99)
                            {
                                sp.SpLevel = 99;
                                sp.XP = 0;
                                session.SendPacket(session.Character.GenerateLev());
                                session.Character.LearnSPSkill();
                                session.SendPacket(session.Character.GenerateSki());
                                session.SendPackets(session.Character.GenerateQuicklist());
                                session.Character.SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_LEVELUP"), 0));
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 8), session.Character.PositionX, session.Character.PositionY);
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Your Special Card Level is already 99!"));
                            }
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo("Your need to be transformed for use that!"));
                    }
                }

                if (session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance
                    && (VNum == 5936 || VNum == 5937 || VNum == 5938 || VNum == 5939 || VNum == 5940 || VNum == 5942 ||
                        VNum == 5943 || VNum == 5944 || VNum == 5945 || VNum == 5946))
                {
                    return;
                }

                if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                 && VNum != 5936 && VNum != 5937 && VNum != 5938 && VNum != 5939 && VNum != 5940 && VNum != 5942 &&
                    VNum != 5943 && VNum != 5944 && VNum != 5945 && VNum != 5946)
                {
                    return;
                }

                if (BCards.Count > 0 && Effect != 1000)
                {
                    if (BCards.Any(s => s.Type == (byte)BCardType.CardType.Buff && s.SubType == 11 &&
                                        new Buff((short)s.SecondData, session.Character.Level).Card.BCards.Any(newbuff =>
                                           session.Character.Buff.GetAllItems().Any(b => b.Card.BCards.Any(buff =>
                                               buff.CardId != newbuff.CardId
                                               && (buff.Type == 33 && buff.SubType == 51 &&
                                                   (newbuff.Type == 33 || newbuff.Type == 58) || newbuff.Type == 33 &&
                                                                                              newbuff.SubType == 51 &&
                                                                                              (buff.Type == 33 ||
                                                                                               buff.Type == 58)
                                                                                              || buff.Type == 33 &&
                                                                                              (buff.SubType == 11 ||
                                                                                               buff.SubType == 31) &&
                                                                                              newbuff.Type == 58 &&
                                                                                              newbuff.SubType == 11 ||
                                                                                              buff.Type == 33 &&
                                                                                              (buff.SubType == 21 ||
                                                                                               buff.SubType == 41) &&
                                                                                              newbuff.Type == 58 &&
                                                                                              newbuff.SubType == 31
                                                                                              || newbuff.Type == 33 &&
                                                                                              (newbuff.SubType == 11 ||
                                                                                               newbuff.SubType == 31) &&
                                                                                              buff.Type == 58 &&
                                                                                              buff.SubType == 11 ||
                                                                                              newbuff.Type == 33 &&
                                                                                              (newbuff.SubType == 21 ||
                                                                                               newbuff.SubType == 41) &&
                                                                                              buff.Type == 58 &&
                                                                                              buff.SubType == 31
                                                                                              || buff.Type == 33 &&
                                                                                              newbuff.Type == 33 &&
                                                                                              buff.SubType ==
                                                                                              newbuff.SubType ||
                                                                                              buff.Type == 58 &&
                                                                                              newbuff.Type == 58 &&
                                                                                              buff.SubType ==
                                                                                              newbuff.SubType))))))
                    {
                        return;
                    }

                    BCards.ForEach(c => c.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    return;
                }

                switch (Effect)
                {

                    case 11007:
                        switch (EffectValue)
                        {
                            case 1:
                                int rnd = ServerManager.RandomNumber(0, 100);
                                if (EffectValue == 1 && byte.TryParse(packetsplit[9], out byte islot2))
                                {
                                    ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot2, InventoryType.Equipment);
                                    if (wearInstance == null)
                                    {
                                        return;
                                    }
                                    switch (wearInstance.Item.VNum)
                                    {
                                        case 4129:
                                            if (rnd < 10)
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                                session.Character.Inventory.RemoveItemAmount(4129, 1);
                                                session.Character.GiftAdd(11003, 1);
                                                MessageExtension.SendBubble(session, "The Upgrade succeeded!");
                                                session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3006),
                                                    session.Character.PositionX, session.Character.PositionY);
                                                return;
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                            MessageExtension.SendBubble(session, "The Upgrade failed!");
                                            break;

                                        case 4130:
                                            if (rnd < 10)
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                                session.Character.Inventory.RemoveItemAmount(4130, 1);
                                                session.Character.GiftAdd(11004, 1);
                                                MessageExtension.SendBubble(session, "The Upgrade succeeded!");
                                                session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3006),
                                                    session.Character.PositionX, session.Character.PositionY);
                                                return;
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                            MessageExtension.SendBubble(session, "The Upgrade failed!");
                                            break;

                                        case 4131:
                                            if (rnd < 10)
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                                session.Character.Inventory.RemoveItemAmount(4131, 1);
                                                session.Character.GiftAdd(11005, 1);
                                                MessageExtension.SendBubble(session, "The Upgrade succeeded!");
                                                session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3006),
                                                    session.Character.PositionX, session.Character.PositionY);
                                                return;
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                            MessageExtension.SendBubble(session, "The Upgrade failed!");
                                            break;

                                        case 4132:
                                            if (rnd < 10)
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                                session.Character.Inventory.RemoveItemAmount(4132, 1);
                                                session.Character.GiftAdd(11006, 1);
                                                MessageExtension.SendBubble(session, "The Upgrade succeeded!");
                                                session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3006),
                                                    session.Character.PositionX, session.Character.PositionY);
                                                return;
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                                            MessageExtension.SendBubble(session, "The Upgrade failed!");
                                            break;
                                    }

                                }
                                break;
                        }
                        break;

                    case 11000:
                        switch (session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                session.SendPacket("info You can not use this Item!");
                                break;

                            case ClassType.Swordsman:
                                session.Character.GiftAdd(901, 1, 1);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                break;

                            case ClassType.Archer:
                                session.Character.GiftAdd(903, 1, 1);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                break;

                            case ClassType.Magician:
                                session.Character.GiftAdd(905, 1, 1);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                break;

                            case ClassType.MartialArtist:
                                session.SendPacket("info You can not use this Item!");
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                break;
                        }
                        break;

                    case 11001:
                        if (!session.Character.StarterBoxUsed)
                        {
                            //Starter Pack
                            session.Character.GiftAdd(9045, 1);
                            session.Character.GiftAdd(9046, 1);
                            session.Character.GiftAdd(9143, 1);
                            session.Character.GiftAdd(8016, 1);
                            session.Character.GiftAdd(8027, 1);
                            session.Character.GiftAdd(4825, 1);
                            session.Character.GiftAdd(9346, 1);

                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.StarterBoxUsed = true;
                            session.SendPacket("info Congratulations! You received your rewards from the Starter Reward Box");
                        }
                        else
                        {
                            session.SendPacket("info This Item is a one-time-use only.");
                        }
                        break;

                    case 11002:
                        if (session.Character.BuffCharge > 0)
                        {
                            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
                            {
                                session.SendPacket("info You cannot do that here.");
                                return;
                            }
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 7405), session.Character.PositionX, session.Character.PositionY);
                            Thread.Sleep(200);
                            session.Character.AddBuff(new Buff(152, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(153, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(155, session.Character.Level), session.Character.BattleEntity);
                            Thread.Sleep(300);
                            session.Character.AddBuff(new Buff(139, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(89, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(91, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(67, session.Character.Level), session.Character.BattleEntity);
                            Thread.Sleep(300);
                            session.Character.AddBuff(new Buff(134, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(157, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(712, session.Character.Level), session.Character.BattleEntity);
                            session.Character.AddBuff(new Buff(713, session.Character.Level), session.Character.BattleEntity);
                            session.Character.BuffCharge -= 1;
                            MessageExtension.SendYellow(session, $"Remaining Buff Charges: {session.Character.BuffCharge}");
                        }
                        else
                        {
                            session.SendPacket("msg 4 You do not have any Buff Charges left!");
                        }
                        break;


                    //Costume Fusion
                    case 10002:
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, 79));
                        break;

                    //Family Experience Booster
                    case 10001:
                        string Message = "This Item cannot be used as your Family already hit the maximum Level!";
                        if (session.Character.Family.FamilyLevel == 20)
                        {
                            session.SendPacket(Message);
                            return;
                        }
                        session.Character.GenerateFamilyXp(EffectValue);
                        session.SendPacket($"info {EffectValue} Family Experience have been added.");
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    //Dragon Gems
                    case 7001:
                        switch (EffectValue)
                        {
                            case 1:
                                if (EffectValue == 1 && byte.TryParse(packetsplit[9], out byte islot2))
                                {
                                    ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot2, InventoryType.Equipment);
                                    if (wearInstance == null)
                                    {
                                        return;
                                    }
                                    if (wearInstance.Upgrade < 20)
                                    {
                                        session.SendPacket("msg 4 The Specialist doesnt have the needed Upgrade");
                                        return;
                                    }
                                    wearInstance.Plus20Buff = 1;
                                    session.SendPacket("msg 4 The Fire Dragon Gem has been added");
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                                break;

                            case 2:
                                if (EffectValue == 2 && byte.TryParse(packetsplit[9], out byte islot3))
                                {
                                    ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot3, InventoryType.Equipment);
                                    if (wearInstance == null)
                                    {
                                        return;
                                    }
                                    if (wearInstance.Upgrade < 20)
                                    {
                                        session.SendPacket("msg 4 The Specialist doesnt have the needed Upgrade");
                                        return;
                                    }
                                    wearInstance.Plus20Buff = 2;
                                    session.SendPacket("msg 4 The Ice Dragon Gem has been added");
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                                break;

                            case 3:
                                if (EffectValue == 3 && byte.TryParse(packetsplit[9], out byte islot4))
                                {
                                    ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot4, InventoryType.Equipment);
                                    if (wearInstance == null)
                                    {
                                        return;
                                    }
                                    if (wearInstance.Upgrade < 20)
                                    {
                                        session.SendPacket("msg 4 The Specialist doesnt have the needed Upgrade");
                                        return;
                                    }
                                    wearInstance.Plus20Buff = 3;
                                    session.SendPacket("msg 4 The Moonlight Dragon Gem has been added");
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                                break;

                            case 4:
                                if (EffectValue == 4 && byte.TryParse(packetsplit[9], out byte islot5))
                                {
                                    ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot5, InventoryType.Equipment);
                                    if (wearInstance == null)
                                    {
                                        return;
                                    }
                                    if (wearInstance.Upgrade < 20)
                                    {
                                        session.SendPacket("msg 4 The Specialist doesnt have the needed Upgrade");
                                        return;
                                    }
                                    wearInstance.Plus20Buff = 4;
                                    session.SendPacket("msg 4 The Sky Dragon Gem has been added");
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                                break;
                        }
                        break;

                    // Change class
                    case 10000:
                        {
                            byte classEquip = (byte)(session.Character.Class == ClassType.Swordsman ? 2 : session.Character.Class == ClassType.Archer ? 4 : session.Character.Class == ClassType.Magician ? 8 : 16);

                            if (session.Character.Inventory.Any(i => i.Type == InventoryType.Wear))
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                                return;
                            }

                            // 2 = swordmen, 4 archer, 8 magician
                            switch (EffectValue)
                            {
                                // Change to Swordmen
                                case 0:
                                    {
                                        if (session.Character.Class == ClassType.Adventurer || session.Character.Class == ClassType.Swordsman)
                                        {
                                            session.SendPacket(session.Character.GenerateSay("You can't use that!", 12));
                                            return;
                                        }

                                        List<ItemInstance> items = session.Character.Inventory.GetAllItems().Where(i => i.Item.Class == classEquip && i.Type != InventoryType.Wear).ToList();
                                        foreach (ItemInstance item in items)
                                        {
                                            short newItemVNum = ServerManager.GetChangeItem(2, item.Item.LevelMinimum, item.Item.LevelJobMinimum,
                                                item.Item.IsHeroic, item.Item.EquipmentSlot, item.Item.ReputationMinimum, item.Item.ItemType, item.Item.ItemSubType, item.Item.Morph, item.Item.ItemValidTime);

                                            if (newItemVNum != 0)
                                            {
                                                ItemInstance itemToChange = session.Character.Inventory.AddNewToInventory(newItemVNum, 1, item.Type, item.Rare,
                                                item.Upgrade, item.Design, item.SpDamage, item.SpDefence, item.SpElement, item.SpHP, item.SpFire, item.SpWater, item.SpLight, item.SpDark, item.SpStoneUpgrade, item.SpLevel).FirstOrDefault();

                                                if (item.ShellEffects?.Count > 0)
                                                {
                                                    itemToChange.ShellEffects.AddRange(item.ShellEffects);

                                                    DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                if (item.RuneEffects?.Count > 0)
                                                {
                                                    itemToChange.RuneEffects.AddRange(item.RuneEffects);

                                                    DAOFactory.RuneEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                itemToChange.SetRarityPoint();
                                                session.Character.DeleteItem(item.Type, item.Slot);
                                                session.SendPacket(itemToChange.GenerateInventoryAdd());
                                            }
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.ChangeClass(ClassType.Swordsman, true);
                                    }
                                    break;

                                // Change to Archer
                                case 1:
                                    {
                                        if (session.Character.Class == ClassType.Adventurer || session.Character.Class == ClassType.Archer)
                                        {
                                            session.SendPacket(session.Character.GenerateSay("You can't use that!", 12));
                                            return;
                                        }

                                        List<ItemInstance> items = session.Character.Inventory.GetAllItems().Where(i => i.Item.Class == classEquip).ToList();
                                        foreach (ItemInstance item in items)
                                        {
                                            short newItemVNum = ServerManager.GetChangeItem(4, item.Item.LevelMinimum, item.Item.LevelJobMinimum,
                                                item.Item.IsHeroic, item.Item.EquipmentSlot, item.Item.ReputationMinimum, item.Item.ItemType, item.Item.ItemSubType, item.Item.Morph, item.Item.ItemValidTime);

                                            if (newItemVNum != 0)
                                            {
                                                ItemInstance itemToChange = session.Character.Inventory.AddNewToInventory(newItemVNum, 1, item.Type, item.Rare,
                                                item.Upgrade, item.Design, item.SpDamage, item.SpDefence, item.SpElement, item.SpHP, item.SpFire, item.SpWater, item.SpLight, item.SpDark, item.SpStoneUpgrade, item.SpLevel).FirstOrDefault();

                                                if (item.ShellEffects?.Count > 0)
                                                {
                                                    itemToChange.ShellEffects.AddRange(item.ShellEffects);

                                                    DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                if (item.RuneEffects?.Count > 0)
                                                {
                                                    itemToChange.RuneEffects.AddRange(item.RuneEffects);

                                                    DAOFactory.RuneEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                itemToChange.SetRarityPoint();
                                                session.Character.DeleteItem(item.Type, item.Slot);
                                                session.SendPacket(itemToChange.GenerateInventoryAdd());
                                            }
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.ChangeClass(ClassType.Archer, true);
                                    }
                                    break;

                                // Change to Mage
                                case 2:
                                    {
                                        if (session.Character.Class == ClassType.Adventurer || session.Character.Class == ClassType.Magician)
                                        {
                                            session.SendPacket(session.Character.GenerateSay("You can't use that!", 12));
                                            return;
                                        }

                                        List<ItemInstance> items = session.Character.Inventory.GetAllItems().Where(i => i.Item.Class == classEquip).ToList();
                                        foreach (ItemInstance item in items)
                                        {
                                            short newItemVNum = ServerManager.GetChangeItem(8, item.Item.LevelMinimum, item.Item.LevelJobMinimum,
                                                item.Item.IsHeroic, item.Item.EquipmentSlot, item.Item.ReputationMinimum, item.Item.ItemType, item.Item.ItemSubType, item.Item.Morph, item.Item.ItemValidTime);

                                            if (newItemVNum != 0)
                                            {
                                                ItemInstance itemToChange = session.Character.Inventory.AddNewToInventory(newItemVNum, 1, item.Type, item.Rare,
                                                item.Upgrade, item.Design, item.SpDamage, item.SpDefence, item.SpElement, item.SpHP, item.SpFire, item.SpWater, item.SpLight, item.SpDark, item.SpStoneUpgrade, item.SpLevel).FirstOrDefault();

                                                if (item.ShellEffects?.Count > 0)
                                                {
                                                    itemToChange.ShellEffects.AddRange(item.ShellEffects);

                                                    DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                if (item.RuneEffects?.Count > 0)
                                                {
                                                    itemToChange.RuneEffects.AddRange(item.RuneEffects);

                                                    DAOFactory.RuneEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                itemToChange.SetRarityPoint();
                                                session.Character.DeleteItem(item.Type, item.Slot);
                                                session.SendPacket(itemToChange.GenerateInventoryAdd());
                                            }
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.ChangeClass(ClassType.Magician, true);
                                    }
                                    break;

                                // Change to Martial
                                case 3:
                                    {
                                        if (session.Character.Class == ClassType.Adventurer || session.Character.Class == ClassType.MartialArtist || session.Character.Level < 80)
                                        {
                                            session.SendPacket(session.Character.GenerateSay("You can't use that!", 12));
                                            return;
                                        }

                                        List<ItemInstance> items = session.Character.Inventory.GetAllItems().Where(i => i.Item.Class == classEquip).ToList();
                                        foreach (ItemInstance item in items)
                                        {
                                            short newItemVNum = ServerManager.GetChangeItem(16, item.Item.LevelMinimum, item.Item.LevelJobMinimum,
                                                item.Item.IsHeroic, item.Item.EquipmentSlot, item.Item.ReputationMinimum, item.Item.ItemType, item.Item.ItemSubType, item.Item.Morph, item.Item.ItemValidTime);

                                            if (newItemVNum != 0)
                                            {

                                                ItemInstance itemToChange = session.Character.Inventory.AddNewToInventory(newItemVNum, 1, item.Type, item.Rare,
                                                item.Upgrade, item.Design, item.SpDamage, item.SpDefence, item.SpElement, item.SpHP, item.SpFire, item.SpWater, item.SpLight, item.SpDark, item.SpStoneUpgrade, item.SpLevel).FirstOrDefault();
                                                if (item.ShellEffects?.Count > 0)
                                                {
                                                    itemToChange.ShellEffects.AddRange(item.ShellEffects);

                                                    DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                if (item.RuneEffects?.Count > 0)
                                                {
                                                    itemToChange.RuneEffects.AddRange(item.RuneEffects);

                                                    DAOFactory.RuneEffectDAO.DeleteByEquipmentSerialId(item.EquipmentSerialId);
                                                }

                                                itemToChange.SetRarityPoint();
                                                session.Character.DeleteItem(item.Type, item.Slot);
                                                session.SendPacket(itemToChange.GenerateInventoryAdd());
                                            }
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.ChangeClass(ClassType.MartialArtist, true);
                                    }
                                    break;
                            }

                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;


                    case 11004:
                        if (session.Character.HasBuff(4004))
                        {
                            session.SendPacket("msg 4 This Effect is already active");
                            return;
                        }
                        else
                        {
                            session.Character.AddBuff(new Buff(4004, session.Character.Level), session.Character.BattleEntity);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;

                    case 201: //Erenias Medal
                        {
                            if (session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.EreniaMedal))
                            {
                                session.SendPacket(session.Character.GenerateSay("This Item is already in use!", 11));
                                return;
                            }

                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddDays(30),
                                StaticBonusType = StaticBonusType.EreniaMedal
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateExts());
                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }
                        break;

                    case 605:
                        if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.Extension))
                        {
                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddDays(VNum == 5795 ? 30 : 60),
                                StaticBonusType = StaticBonusType.Extension
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateExts());
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }

                        break;

                    case 604:
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(1),
                            StaticBonusType = StaticBonusType.Extension
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        break;

                    // Honour Medals
                    case 69:
                        {
                            session.Character.Reputation += ReputPrice;
                            session.SendPacket(session.Character.GenerateFd());
                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REPUT_INCREASE"), ReputPrice), 11));
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            break;
                        }
                    // TimeSpace Stones
                    case 140:
                        if (ServerManager.Instance.ChannelId == 51 || session.Character.MapInstance.MapInstanceType == MapInstanceType.ArenaInstance
                           || session.Character.MapInstance.MapInstanceType == MapInstanceType.CaligorInstance
                           || session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 0));
                            return;
                        }
                        if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            if (ServerManager.Instance.TimeSpaces.FirstOrDefault(s => s.Id == EffectValue) is ScriptedInstance timeSpace)
                            {
                                session.Character.EnterInstance(timeSpace);
                                //session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }

                        }
                        break;

                    // SP Potions
                    case 150:
                    case 151:
                        {
                            if (session.Character.SpAdditionPoint >= 1000000)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_POINTS_FULL"), 0));
                                break;
                            }

                            session.Character.SpAdditionPoint += EffectValue;

                            if (session.Character.SpAdditionPoint > 1000000)
                            {
                                session.Character.SpAdditionPoint = 1000000;
                            }

                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"), EffectValue), 0));
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateSpPoint());
                        }
                        break;

                    // Specialist Medal
                    case 204:
                        {
                            if (session.Character.SpPoint >= 10000 && session.Character.SpAdditionPoint >= 1000000)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_POINTS_FULL"), 0));
                                break;
                            }

                            session.Character.SpPoint += EffectValue;

                            if (session.Character.SpPoint > 10000)
                            {
                                session.Character.SpPoint = 10000;
                            }

                            session.Character.SpAdditionPoint += EffectValue * 3;

                            if (session.Character.SpAdditionPoint > 1000000)
                            {
                                session.Character.SpAdditionPoint = 1000000;
                            }

                            session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDEDBOTH"), EffectValue, EffectValue * 3), 0));
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateSpPoint());
                        }
                        break;

                    // Raid Seals
                    case 301:
                        RaidSealThread.Run(session, inv);
                        break;

                    case 409:
                        try
                        {
                            RaidboxThread.GenerateReward(session, inv);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        catch (Exception ex)
                        {
                            ////await //LOGGER(ex.ToString());
                        }
                        break;

                    //Namechange
                    case 5787:
                        if (session.Character.Group != null && session.Character.Inventory.Any(x => x.Item.VNum == 5787)) // Enter VNum
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg("Leave your group to change your name", 0));
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateInbox($"#glmk^ 14 1 Charactername Charactername"));
                        }
                        break;

                    // Partner Suits/Skins
                    case 305:
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == int.Parse(packetsplit[3]));
                        if (mate != null && EffectValue == mate.NpcMonsterVNum && mate.Skin == 0)
                        {
                            mate.Skin = Morph;
                            session.SendPacket(mate.GenerateCMode(mate.Skin));
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;

                    //suction Funnel (Quest Item / QuestId = 1724)
                    case 400:
                        if (session.Character == null || session.Character.Quests.All(q => q.QuestId != 1724))
                        {
                            break;
                        }

                        if (session.Character.Quests.FirstOrDefault(q => q.QuestId == 1724) is CharacterQuest kenkoQuest)
                        {
                            var kenko = session.CurrentMapInstance?.Monsters.FirstOrDefault(m => m.MapMonsterId == session.Character.LastNpcMonsterId && m.MonsterVNum > 144 && m.MonsterVNum < 154);

                            if (kenko == null || session.Character.Inventory.CountItem(1174) > 0)
                            {
                                break;
                            }

                            if (session.Character.LastFunnelUse.AddSeconds(30) <= DateTime.Now)
                            {
                                if (kenko.CurrentHp / kenko.MaxHp * 100 < 30)
                                {
                                    if (ServerManager.RandomNumber() < 30)
                                    {
                                        kenko.SetDeathStatement();
                                        session.Character.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, kenko.MapMonsterId));
                                        session.Character.Inventory.AddNewToInventory(1174); // Kenko Bead
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KENKO_CATCHED"), 0));
                                    }
                                    else
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_CATCH_FAIL"), 0));
                                    }
                                    session.Character.LastFunnelUse = DateTime.Now;
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HP_TOO_HIGH"), 0));
                                }
                            }
                        }

                        break;

                    // Fairy Booster
                    case 250:
                        if (!session.Character.Buff.ContainsKey(131))
                        {
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 131 });
                            session.CurrentMapInstance?.Broadcast(session.Character.GeneratePairy());
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), inv.Item.Name), 0));
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.PositionX, session.Character.PositionY);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }
                        break;

                    // Energy Booster
                    case 251:
                        if (!session.Character.Buff.ContainsKey(5000))
                        {
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5000 });
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), inv.Item.Name), 0));
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.PositionX, session.Character.PositionY);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }
                        break;

                    // Rainbow Pearl/Magic Eraser
                    case 666:
                        if (EffectValue == 1 && byte.TryParse(packetsplit[9], out var islot))
                        {
                            var wearInstance = session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);

                            if (wearInstance != null &&
                                (wearInstance.Item.ItemType == ItemType.Weapon || wearInstance.Item.ItemType == ItemType.Armor) && wearInstance.ShellEffects.Count != 0 && !wearInstance.Item.IsHeroic)
                            {
                                wearInstance.ShellEffects.Clear();
                                wearInstance.ShellRarity = null;
                                DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(wearInstance.EquipmentSerialId);
                                if (wearInstance.EquipmentSerialId == Guid.Empty)
                                {
                                    wearInstance.EquipmentSerialId = Guid.NewGuid();
                                }

                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OPTION_DELETE"), 0));
                            }
                        }
                        else
                        {
                            session.SendPacket("guri 18 0");
                        }

                        break;

                    case 7482:
                        if (EffectValue == 62 && byte.TryParse(packetsplit[9], out byte rslot))
                        {
                            ItemInstance cellonbonus = session.Character.Inventory.LoadBySlotAndType(rslot, InventoryType.Equipment);

                            if (cellonbonus != null && cellonbonus.Item.ItemType == ItemType.Jewelery && cellonbonus.CellonOptions.Count != 0)
                            {
                                if (cellonbonus == null)
                                {
                                    return;
                                }
                                cellonbonus.CellonOptions.Clear();
                                DAOFactory.CellonOptionDAO.DeleteByEquipmentSerialId(cellonbonus.EquipmentSerialId);
                                if (cellonbonus.EquipmentSerialId == Guid.Empty)
                                {
                                    cellonbonus.EquipmentSerialId = Guid.NewGuid();
                                }
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        break;

                    // Atk/Def/HP/Exp potions
                    case 6600:
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;


                    // Ancelloan's Blessing
                    case 208:
                        if (!session.Character.Buff.ContainsKey(121) && !session.Character.Buff.ContainsKey(4044))
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 121 });
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));

                        }
                        break;

                    case 2457:
                        switch (EffectValue)
                        {
                            case 0:
                                //add delay
                                if (session.Character.MapId > 0)
                                {
                                    int dist = Map.GetDistance(
                                        new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY },
                                        new MapCell { X = 120, Y = 56 });

                                    int dist1 = Map.GetDistance(
                                        new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY },
                                        new MapCell { X = 120, Y = 56 });

                                    if (dist < 6)
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 820));
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 821));
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6008));
                                    }
                                    if (dist < 3 && session.Character.MapId == 1)
                                    {
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 822));
                                        Event.PTS.GeneratePTS(1805, session);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    if (dist < 1 && session.Character.MapId == 5)
                                    {
                                        Event.PTS.GeneratePTS(1824, session);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    if (dist < 15)
                                    {
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 820));
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6008));
                                    }
                                    else
                                    {
                                        //say 1 521919 10 Aucun signal ne peut être reçu, car la distance est trop élevée.
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 820));
                                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6009));
                                    }
                                }
                                break;
                        }
                        break;

                    case 5836://Libreta del banco
                        session.SendPacket($"gb 0 {session.Character.GoldBank / 1000} {session.Character.Gold} 0 0");
                        session.SendPacket($"s_memo 6 [Account balance]: {session.Character.GoldBank} gold; [Owned]: {session.Character.Gold} gold\nWe will do our best. Thank you for using the services of Cuarry Bank.");
                        break;


                    // Prevent usage of items
                    case 215:
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 0));
                            return;
                        }

                    // Valentine Buff
                    case 209:
                        if (!session.Character.Buff.ContainsKey(109))
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                            //session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 109 });
                            session.Character.AddBuff(new Buff(109, session.Character.Level),
                                session.Character.BattleEntity);
                        }
                        else
                        {
                            session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }

                        break;

                    // Valentine Buff, but stronger
                    case 299:
                        if (!session.Character.Buff.ContainsKey(109) || !session.Character.Buff.ContainsKey(244))
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.AddBuff(new Buff(244, session.Character.Level), session.Character.BattleEntity);

                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }
                        break;

                    // Guardian Angel's Blessing
                    case 210:
                        if (!session.Character.Buff.ContainsKey(122))
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 122 });
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }

                        break;

                    case 2081:
                        if (!session.Character.Buff.ContainsKey(146))
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                        }
                        break;

                    // Divorce letter
                    case 6969:
                        if (session.Character.Group != null)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ALLOWED_IN_GROUP"), 0));
                            return;
                        }

                        var rel = session.Character.CharacterRelations.FirstOrDefault(s => s.RelationType == CharacterRelationType.Spouse);
                        if (rel != null)
                        {
                            session.Character.DeleteRelation(
                                rel.CharacterId == session.Character.CharacterId ? rel.RelatedCharacterId : rel.CharacterId,
                                CharacterRelationType.Spouse);
                            session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("DIVORCED")));
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }

                        break;

                    // Cupid's arrow
                    case 34:
                        if (packetsplit != null && packetsplit.Length > 3)
                        {
                            if (long.TryParse(packetsplit[3], out var characterId))
                            {
                                if (session.Character.CharacterId == characterId)
                                {
                                    return;
                                }

                                if (session.Character.CharacterRelations.Any(s =>
                                        s.RelationType == CharacterRelationType.Spouse))
                                {
                                    session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_MARRIED")}");
                                    return;
                                }

                                if (session.Character.Group != null)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("NOT_ALLOWED_IN_GROUP"), 0));
                                    return;
                                }

                                if (!session.Character.IsFriendOfCharacter(characterId))
                                {
                                    session.SendPacket($"info {Language.Instance.GetMessageFromKey("MUST_BE_FRIENDS")}");
                                    return;
                                }

                                var otherSession = ServerManager.Instance.GetSessionByCharacterId(characterId);
                                if (otherSession != null)
                                {
                                    if (otherSession.Character.Group != null)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("OTHER_PLAYER_IN_GROUP"), 0));
                                        return;
                                    }

                                    otherSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                            $"#fins^34^{session.Character.CharacterId} #fins^69^{session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("MARRY_REQUEST"), session.Character.Name)}"));
                                    session.Character.MarryRequestCharacters.Add(characterId);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                            }
                        }

                        break;

                    case 100: // Miniland Signpost //Soon readded
                        {
                            if (session.Character.BattleEntity.GetOwnedNpcs()
                                .Any(s => session.Character.BattleEntity.IsSignpost(s.NpcVNum)))
                            {
                                return;
                            }

                            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance &&
                                new short[] { 1, 145 }.Contains(session.CurrentMapInstance.Map.MapId))
                            {
                                var signPost = new MapNpc
                                {
                                    NpcVNum = (short)EffectValue,
                                    MapX = session.Character.PositionX,
                                    MapY = session.Character.PositionY,
                                    MapId = session.CurrentMapInstance.Map.MapId,
                                    ShouldRespawn = false,
                                    IsMoving = false,
                                    MapNpcId = session.CurrentMapInstance.GetNextNpcId(),
                                    Owner = session.Character.BattleEntity,
                                    Dialog = 10000,
                                    Position = 2,
                                    Name = $"{session.Character.Name}'s^[Miniland]"
                                };
                                switch (EffectValue)
                                {
                                    case 1428:
                                    case 1499:
                                    case 1519:
                                        signPost.AliveTime = 3600;
                                        break;

                                    default:
                                        signPost.AliveTime = 1800;
                                        break;
                                }

                                signPost.Initialize(session.CurrentMapInstance);
                                session.CurrentMapInstance.AddNPC(signPost);
                                session.CurrentMapInstance.Broadcast(signPost.GenerateIn());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        break;

                    case 550: // Campfire and other craft npcs //SoonReadded
                        {
                            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                            {
                                short dialog = 10023;
                                switch (EffectValue)
                                {
                                    case 956:
                                        dialog = 10023;
                                        break;

                                    case 957:
                                        dialog = 10024;
                                        break;

                                    case 959:
                                        dialog = 10026;
                                        break;
                                }

                                var campfire = new MapNpc
                                {
                                    NpcVNum = (short)EffectValue,
                                    MapX = session.Character.PositionX,
                                    MapY = session.Character.PositionY,
                                    MapId = session.CurrentMapInstance.Map.MapId,
                                    ShouldRespawn = false,
                                    IsMoving = false,
                                    MapNpcId = session.CurrentMapInstance.GetNextNpcId(),
                                    Owner = session.Character.BattleEntity,
                                    Dialog = dialog,
                                    Position = 2
                                };
                                campfire.AliveTime = 180;
                                campfire.Initialize(session.CurrentMapInstance);
                                session.CurrentMapInstance.AddNPC(campfire);
                                session.CurrentMapInstance.Broadcast(campfire.GenerateIn());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        break;

                    // Faction Egg
                    case 570:
                        if (session.Character.Faction == (FactionType)EffectValue)
                        {
                            return;
                        }

                        if (EffectValue < 3)
                        {
                            session.SendPacket(session.Character.Family == null
                                    ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                                    : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("IN_FAMILY"),
                                            0));
                        }
                        else
                        {
                            session.SendPacket(session.Character.Family != null
                                    ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                                    : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY"),
                                            0));
                        }
                        break;

                    // SP Wings
                    case 650:
                        var SpecialistInstance =
                            session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                        if (session.Character.UseSp && SpecialistInstance != null && !session.Character.IsSeal)
                        {
                            if (Option == 0)
                            {
                                session.SendPacket(
                                    $"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_WINGS_CHANGE")}");
                            }
                            else
                            {
                                if (!SpecialistInstance.ChangedWings)
                                {
                                    SpecialistInstance.Design = (byte)EffectValue;
                                    SpecialistInstance.WingBuff = (byte)EffectValue;
                                    SpecialistInstance.OriginalWings = VNum;
                                    session.Character.MorphUpgrade2 = EffectValue;
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                    session.SendPacket(session.Character.GenerateStat());
                                    session.SendPackets(session.Character.GenerateStatChar());
                                }
                                if (SpecialistInstance.ChangedWings)
                                {
                                    SpecialistInstance.WingBuff = (byte)EffectValue;
                                    SpecialistInstance.OriginalWings = VNum;
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                    session.SendPacket(session.Character.GenerateStat());
                                    session.SendPackets(session.Character.GenerateStatChar());
                                }
                                WingsThread.RemoveBuff(session);
                                CharacterHelper.AddSpecialistWingsBuff(session);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"),
                                0));
                        }

                        break;

                    case 7389:
                        if (session.Character.Class == ClassType.Swordsman)
                        {
                            var rnd = ServerManager.RandomNumber(0, 2);
                            if (rnd <= 100)
                            {
                                short[] vnums = { 7383, 7384 };
                                byte[] counts = { 1, 1 };
                                var item = ServerManager.RandomNumber(0, 2);
                                session.Character.GiftAdd(vnums[item], counts[item]);
                                session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                                session.Character.NoAttack = false;
                                session.Character.NoMove = false;
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else if (session.Character.Class == ClassType.Archer)
                        {
                            var rnd = ServerManager.RandomNumber(0, 2);
                            if (rnd <= 100)
                            {
                                short[] vnums = { 7385, 7386 };
                                byte[] counts = { 1, 1 };
                                var item = ServerManager.RandomNumber(0, 2);
                                session.Character.GiftAdd(vnums[item], counts[item]);
                                session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                                session.Character.NoAttack = false;
                                session.Character.NoMove = false;
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else if (session.Character.Class == ClassType.Magician)
                        {
                            var rnd = ServerManager.RandomNumber(0, 2);
                            if (rnd <= 100)
                            {
                                short[] vnums = { 7387, 7388 };
                                byte[] counts = { 1, 1 };
                                var item = ServerManager.RandomNumber(0, 2);
                                session.Character.GiftAdd(vnums[item], counts[item]);
                                session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                                session.Character.NoAttack = false;
                                session.Character.NoMove = false;
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else
                        {
                            var rnd = ServerManager.RandomNumber(0, 6);
                            if (rnd <= 100)
                            {
                                short[] vnums = { 7383, 7384, 7385, 7386, 7387, 7388 };
                                byte[] counts = { 1, 1, 1, 1, 1, 1 };
                                var item = ServerManager.RandomNumber(0, 6);
                                session.Character.GiftAdd(vnums[item], counts[item]);
                                session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                                session.Character.NoAttack = false;
                                session.Character.NoMove = false;
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        break;

                    case 7402:
                        if (session.Character.Gender == GenderType.Male)
                        {
                            session.Character.GiftAdd(7390, 1);
                            session.Character.GiftAdd(7396, 1);
                            session.Character.GiftAdd(7190, 15);
                            session.Character.GiftAdd(5892, 5);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 20);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.Character.GiftAdd(7393, 1);
                            session.Character.GiftAdd(7399, 1);
                            session.Character.GiftAdd(7190, 15);
                            session.Character.GiftAdd(5892, 5);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 20);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;

                    case 7403:
                        if (session.Character.Gender == GenderType.Male)
                        {
                            session.Character.GiftAdd(7391, 1);
                            session.Character.GiftAdd(7397, 1);
                            session.Character.GiftAdd(7190, 20);
                            session.Character.GiftAdd(5892, 7);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 30);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.Character.GiftAdd(7394, 1);
                            session.Character.GiftAdd(7400, 1);
                            session.Character.GiftAdd(7190, 20);
                            session.Character.GiftAdd(5892, 7);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 30);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;

                    case 7404:
                        if (session.Character.Gender == GenderType.Male)
                        {
                            session.Character.GiftAdd(7392, 1);
                            session.Character.GiftAdd(7396, 1);
                            session.Character.GiftAdd(7190, 25);
                            session.Character.GiftAdd(5892, 10);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 50);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.Character.GiftAdd(7395, 1);
                            session.Character.GiftAdd(7401, 1);
                            session.Character.GiftAdd(7190, 25);
                            session.Character.GiftAdd(5892, 10);
                            session.Character.GiftAdd(9337, 1);
                            session.Character.GiftAdd(9339, 1);
                            session.Character.GiftAdd(7338, 50);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;

                    //Rarify Generator
                    case 7407:
                        if (EffectValue == 0 && byte.TryParse(packetsplit[9], out byte sslot))
                        {
                            ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(sslot, InventoryType.Equipment);
                            sbyte rare = (sbyte)ServerManager.RandomNumber(5, 8);
                            if (wearInstance != null && (wearInstance.Item.ItemType == ItemType.Weapon || wearInstance.Item.ItemType == ItemType.Armor) && wearInstance.ShellEffects.Count == 0 && wearInstance.Rare < 8)
                            {
                                wearInstance.Rare = rare;
                                session.SendPacket(wearInstance.GenerateInventoryAdd());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format($"Your Equipment {wearInstance.Item.Name} now is Rare: {rare}, Great!.", rare), 0));
                            }
                            if (wearInstance.ShellEffects.Count > 0)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format($"Your Equipment {wearInstance.Item.Name} have shells, please eraser or extract it."), 0));
                                return;
                            }
                        }
                        break;

                    // Rarity Eraser
                    case 7340:
                        if (EffectValue == 0 && byte.TryParse(packetsplit[9], out byte Islot))
                        {
                            ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(Islot, InventoryType.Equipment);

                            if (wearInstance != null && (wearInstance.Item.ItemType == ItemType.Weapon || wearInstance.Item.ItemType == ItemType.Armor) && wearInstance.Rare < 8)
                            {
                                wearInstance.Rare = 0;
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RARE_SET_0"), 0));
                            }
                            else
                            {
                                return;
                            }
                        }
                        break;


                        //CALIGOR
                    case 5960:
                        {
                            short[] vnums3 = null;
                            vnums3 = new short[] { 2514, 2515, 2516, 2517, 2518, 2519, 2520, 2521, 4490, 1428, 2282, 1030, 9388, 11007 };
                            byte[] counts3 = { 30, 30, 30, 30, 30, 30, 30, 30, 1, 25, 75, 50, 1, 3 };
                            int item3 = ServerManager.RandomNumber(0, 13);
                            session.Character.GiftAdd(vnums3[item3], counts3[item3]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        break;


                    // Set Shell Option
                    case 7338:
                        {
                            if (byte.TryParse(packetsplit[9], out byte itemSlot))
                            {
                                ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(itemSlot, InventoryType.Equipment);

                                if (wearInstance != null && wearInstance.Rare >= 1 && !wearInstance.Item.IsHeroic && wearInstance.Item.LevelMinimum >= 90)
                                {
                                    if (wearInstance.ShellEffects.Count != 0)
                                    {
                                        wearInstance.ShellEffects.Clear();
                                        DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(wearInstance.EquipmentSerialId);
                                        if (wearInstance.EquipmentSerialId == Guid.Empty)
                                        {
                                            wearInstance.EquipmentSerialId = Guid.NewGuid();
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                        wearInstance.SetShellEffects(true, 3);
                                        wearInstance.BoundCharacterId = session?.Character?.CharacterId;
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg("Shell Generated Succesfully!", 0));
                                    }
                                    else
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        wearInstance.SetShellEffects(true, 3);
                                        wearInstance.BoundCharacterId = session?.Character?.CharacterId;
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg("Shell Generated Succesfully!", 0));
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateInfo("Can't use in this Equipment."));
                                    return;
                                }
                            }
                        }
                        break;

                    // Set Shell Heroic Option
                    case 7481:
                        {
                            if (byte.TryParse(packetsplit[9], out byte itemSlot))
                            {
                                ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(itemSlot, InventoryType.Equipment);

                                if (wearInstance != null && wearInstance.Rare >= 1 && wearInstance.Item.IsHeroic)
                                {
                                    if (wearInstance.ShellEffects.Count != 0)
                                    {
                                        wearInstance.ShellEffects.Clear();
                                        DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(wearInstance.EquipmentSerialId);
                                        if (wearInstance.EquipmentSerialId == Guid.Empty)
                                        {
                                            wearInstance.EquipmentSerialId = Guid.NewGuid();
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                        wearInstance.GenerateHeroicShell(RarifyProtection.RandomHeroicAmulet);
                                        wearInstance.BoundCharacterId = session?.Character?.CharacterId;
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg("Shell Heroic Generated Succesfully!", 0));
                                    }
                                    else
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        wearInstance.GenerateHeroicShell(RarifyProtection.RandomHeroicAmulet);
                                        wearInstance.BoundCharacterId = session?.Character?.CharacterId;
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg("Shell Heroic Generated Succesfully!", 0));
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateInfo("Can't use in this Equipment."));
                                    return;
                                }
                            }
                        }
                        break;


                    // Self-Introduction
                    case 203:
                        if (!session.Character.IsVehicled && Option == 0)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 2, session.Character.CharacterId, 1));
                        }
                        break;

                    // Magic Lamp
                    case 651:
                        if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                        {
                            if (Option == 0)
                            {
                                session.SendPacket(
                                    $"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_USE")}");
                            }
                            else
                            {
                                session.Character.Event.EmitEvent(new ChangeSexEvent());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else
                        {
                            session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                        }

                        break;

                    // Vehicles
                    case 1000:
                        if (EffectValue != 0
                            || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.EventGameInstance
                            || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                            || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.IceBreakerInstance
                            || session.Character.IsSeal || session.Character.IsMorphed)
                        {
                            return;
                        }

                        if (session.Character.HasShopOpened)
                        {
                            return;
                        }

                        var morph = Morph;
                        var speed = Speed;
                        if (Morph < 0)
                        {
                            switch (VNum)
                            {
                                case 5923:
                                    morph = 2513;
                                    speed = 14;
                                    break;
                            }
                        }

                        if (!session.Character.IsVehicled && Morph == 2444)
                        {
                            session.Character.AddBuff(new Buff(737, session.Character.Level), session.Character.BattleEntity);
                        }
                        else
                        {
                            session.Character.RemoveBuff(737, true);
                        }

                        if (!session.Character.IsVehicled && Morph == 3711)
                        {
                            session.Character.AddBuff(new Buff(785, session.Character.Level), session.Character.BattleEntity);
                        }
                        else
                        {
                            session.Character.RemoveBuff(785, true);
                        }

                        if (!session.Character.IsVehicled && Morph == 2448)
                        {
                            session.Character.AddBuff(new Buff(764, session.Character.Level), session.Character.BattleEntity);
                        }
                        else
                        {
                            session.Character.RemoveBuff(764, true);
                        }

                        if (morph > 0)
                        {
                            if (Option == 0 && !session.Character.IsVehicled)
                            {
                                if (session.Character.Buff.Any(s => s.Card.BuffType == BuffType.Bad))
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("CANT_TRASFORM_WITH_DEBUFFS"),
                                        0));
                                    return;
                                }

                                if (session.Character.IsSitting)
                                {
                                    session.Character.IsSitting = false;
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateRest());
                                }

                                session.Character.LastDelay = DateTime.Now;
                                session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 3,
                                    $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2"));
                            }
                            else
                            {
                                if (!session.Character.IsVehicled && Option != 0)
                                {
                                    var delay = DateTime.Now.AddSeconds(-4);
                                    if (session.Character.LastDelay > delay &&
                                        session.Character.LastDelay < delay.AddSeconds(2))
                                    {
                                        session.Character.IsVehicled = true;
                                        session.Character.VehicleSpeed = speed;
                                        session.Character.VehicleItem = this;
                                        session.Character.LoadSpeed();
                                        session.Character.MorphUpgrade = 0;
                                        session.Character.MorphUpgrade2 = 0;
                                        session.Character.Morph = morph + (byte)session.Character.Gender;
                                        session.CurrentMapInstance?.Broadcast(
                                            StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId,
                                                196), session.Character.PositionX, session.Character.PositionY);
                                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                        session.SendPacket(session.Character.GenerateCond());
                                        session.Character.LastSpeedChange = DateTime.Now;
                                        session.Character.Mates.Where(s => s.IsTeamMember).ToList()
                                            .ForEach(s => session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                                        if (Morph < 0)
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                    }
                                }
                                else if (session.Character.IsVehicled)
                                {
                                    session.Character.RemoveVehicle();
                                    foreach (var teamMate in session.Character.Mates.Where(m => m.IsTeamMember))
                                    {
                                        teamMate.PositionX =
                                            (short)(session.Character.PositionX +
                                                     (teamMate.MateType == MateType.Partner ? -1 : 1));
                                        teamMate.PositionY = (short)(session.Character.PositionY + 1);
                                        if (session.Character.MapInstance.Map.IsBlockedZone(teamMate.PositionX,
                                            teamMate.PositionY))
                                        {
                                            teamMate.PositionX = session.Character.PositionX;
                                            teamMate.PositionY = session.Character.PositionY;
                                        }

                                        teamMate.UpdateBushFire();
                                        foreach (var sess in session.CurrentMapInstance.Sessions.Where(s =>
                                            s.Character != null))
                                        {
                                            if (ServerManager.Instance.ChannelId != 51 ||
                                                session.Character.Faction == sess.Character.Faction)
                                            {
                                                sess.SendPacket(teamMate.GenerateIn(false,
                                                        ServerManager.Instance.ChannelId == 51));
                                            }
                                            else
                                            {
                                                sess.SendPacket(teamMate.GenerateIn(true,
                                                        ServerManager.Instance.ChannelId == 51, sess.Account.Authority));
                                            }
                                        }
                                    }

                                    session.SendPacket(session.Character.GeneratePinit());
                                    session.Character.Mates.ForEach(s => session.SendPacket(s.GenerateScPacket()));
                                    session.SendPackets(session.Character.GeneratePst());
                                }
                            }
                        }

                        break;

                    // Sealed Vessel
                    case 1002:
                        if (session?.Character?.MapId == 1)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateInfo("WARNING: It is forbidden to use Vessels in Nosville, go to any other map!"));
                            return;
                        }
                        {
                            int type, secondaryType, inventoryType, slot;
                            if (packetsplit != null && int.TryParse(packetsplit[2], out type) &&
                                int.TryParse(packetsplit[3], out secondaryType) &&
                                int.TryParse(packetsplit[4], out inventoryType) && int.TryParse(packetsplit[5], out slot))
                            {
                                int packetType;
                                switch (EffectValue)
                                {
                                    case 69:
                                        if (int.TryParse(packetsplit[6], out packetType))
                                        {
                                            switch (packetType)
                                            {
                                                case 0:
                                                    session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                                            $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                                    break;

                                                case 1:
                                                    var rnd = ServerManager.RandomNumber(0, 1000);
                                                    if (rnd < 5)
                                                    {
                                                        short[] vnums =
                                                        {
                                                        5560, 5591, 4099, 907, 1160, 4705, 4706, 4707, 4708, 4709, 4710,
                                                        4711, 4712, 4713, 4714,
                                                        4715, 4716
                                                };
                                                        byte[] counts = { 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                                                        var item = ServerManager.RandomNumber(0, 17);
                                                        session.Character.GiftAdd(vnums[item], counts[item]);
                                                    }
                                                    else if (rnd < 30)
                                                    {
                                                        short[] vnums = { 361, 362, 363, 366, 367, 368, 371, 372, 373 };
                                                        session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 9)], 1);
                                                    }
                                                    else
                                                    {
                                                        short[] vnums =
                                                        {
                                                        1161, 2282, 1030, 1244, 1218, 5369, 1012, 1363, 1364, 2160, 2173,
                                                        5959, 5983, 2514,
                                                        2515, 2516, 2517, 2518, 2519, 2520, 2521, 1685, 1686, 5087, 5203,
                                                        2418, 2310, 2303,
                                                        2169, 2280, 5892, 5893, 5894, 5895, 5896, 5897, 5898, 5899, 5332,
                                                        5105, 2161, 2162
                                                };
                                                        byte[] counts =
                                                        {
                                                        10, 10, 20, 5, 1, 1, 99, 1, 1, 5, 5, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                                                        1, 1, 1, 1, 5, 20,
                                                        20, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
                                                };
                                                        var item = ServerManager.RandomNumber(0, 42);
                                                        session.Character.GiftAdd(vnums[item], counts[item]);
                                                    }

                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                    break;

                                                case 2: // Santa vessels
                                                    var random = ServerManager.RandomNumber();
                                                    if (random <= 5)
                                                    {
                                                        short[] vnums =
                                                        {
                                                        4075, 4076, 5209, 5211, 5070
                                                };
                                                        byte[] counts = { 5, 5, 40, 40 };
                                                        var item = ServerManager.RandomNumber(0, 5);
                                                        session.Character.GiftAdd(vnums[item], counts[item]);
                                                    }

                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                    break;
                                            }
                                        }

                                        break;

                                    default:
                                        if (int.TryParse(packetsplit[6], out packetType))
                                        {
                                            if (session.Character.MapInstance.Map.MapTypes.Any(s =>
                                                s.MapTypeId == (short)MapTypeEnum.Act4))
                                            {
                                                return;
                                            }

                                            if (session.Account.IsLimited)
                                            {
                                                session.SendPacket(
                                                    UserInterfaceHelper.GenerateInfo(
                                                        Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                                                return;
                                            }

                                            switch (packetType)
                                            {
                                                case 0:
                                                    session.SendPacket(UserInterfaceHelper.GenerateDelay(1000, 7,
                                                        $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                                    break;

                                                case 1:
                                                    if (session.HasCurrentMapInstance &&
                                                        (session.Character.MapInstance == session.Character.Miniland ||
                                                         session.CurrentMapInstance.MapInstanceType ==
                                                         MapInstanceType.BaseMapInstance) &&
                                                        (session.Character.LastVessel.AddSeconds(1) <= DateTime.Now ||
                                                         session.Character.StaticBonusList.Any(s =>
                                                             s.StaticBonusType == StaticBonusType.FastVessels)))
                                                    {
                                                        short[] vnums =
                                                        {
                                                    1386, 1387, 1388, 1389, 1390, 1391, 1392, 1393, 1394, 1395, 1396,
                                                    1397, 1398, 1399, 1400, 1401, 1402, 1403, 1404, 1405, 532, 535, 751, 1424 , 2046, 2047 , 2055, 2056, 2057, 2058
                                                };
                                                        var vnum = vnums[ServerManager.RandomNumber(0, 20)];

                                                        var npcmonster = ServerManager.GetNpcMonster(vnum);
                                                        if (npcmonster == null)
                                                        {
                                                            return;
                                                        }

                                                        var monster = new MapMonster
                                                        {
                                                            MonsterVNum = vnum,
                                                            MapX = session.Character.PositionX,
                                                            MapY = session.Character.PositionY,
                                                            MapId = session.Character.MapInstance.Map.MapId,
                                                            Position = session.Character.Direction,
                                                            IsMoving = true,
                                                            MapMonsterId = session.CurrentMapInstance.GetNextMonsterId(),
                                                            ShouldRespawn = false,
                                                            IsVessel = true
                                                        };
                                                        monster.Initialize(session.CurrentMapInstance);
                                                        session.CurrentMapInstance.AddMonster(monster);
                                                        session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                        session.Character.LastVessel = DateTime.Now;
                                                    }

                                                    break;

                                                case 2: // monsters in --> xmas sealed vessel
                                                    if (session.HasCurrentMapInstance &&
                                                        (session.Character.MapInstance == session.Character.Miniland ||
                                                         session.CurrentMapInstance.MapInstanceType ==
                                                         MapInstanceType.BaseMapInstance) &&
                                                        (session.Character.LastVessel.AddSeconds(1) <= DateTime.Now ||
                                                         session.Character.StaticBonusList.Any(s =>
                                                             s.StaticBonusType == StaticBonusType.FastVessels)))
                                                    {
                                                        short[] vnums =
                                                            {532, 535, 751, 1424, 2046, 2047, 2055, 2056, 2057, 2058};
                                                        var vnum = vnums[ServerManager.RandomNumber(0, 10)];

                                                        var npcmonster = ServerManager.GetNpcMonster(vnum);
                                                        if (npcmonster == null)
                                                        {
                                                            return;
                                                        }

                                                        var monster = new MapMonster
                                                        {
                                                            MonsterVNum = vnum,
                                                            MapX = session.Character.PositionX,
                                                            MapY = session.Character.PositionY,
                                                            MapId = session.Character.MapInstance.Map.MapId,
                                                            Position = session.Character.Direction,
                                                            IsMoving = true,
                                                            MapMonsterId = session.CurrentMapInstance.GetNextMonsterId(),
                                                            ShouldRespawn = false,
                                                            IsVessel = true
                                                        };
                                                        monster.Initialize(session.CurrentMapInstance);
                                                        session.CurrentMapInstance.AddMonster(monster);
                                                        session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                        session.Character.LastVessel = DateTime.Now;
                                                    }

                                                    break;
                                            }
                                        }

                                        break;
                                }
                            }

                            break;
                        }

                    // Golden Bazaar Medal
                    case 1003:
                        if (!session.Character.StaticBonusList.Any(s =>
                            s.StaticBonusType == StaticBonusType.BazaarMedalGold ||
                            s.StaticBonusType == StaticBonusType.BazaarMedalSilver))
                        {
                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddDays(EffectValue),
                                StaticBonusType = StaticBonusType.BazaarMedalGold
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }

                        break;

                    // Silver Bazaar Medal
                    case 1004:
                        if (!session.Character.StaticBonusList.Any(s =>
                            s.StaticBonusType == StaticBonusType.BazaarMedalGold ||
                            s.StaticBonusType == StaticBonusType.BazaarMedalGold))
                        {
                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddDays(EffectValue),
                                StaticBonusType = StaticBonusType.BazaarMedalSilver
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }

                        break;

                    // Pet Slot Expansion
                    case 1006:
                        if (Option == 0)
                        {
                            session.SendPacket(
                                $"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2 {Language.Instance.GetMessageFromKey("ASK_PET_MAX")}");
                        }
                        else if (inv.Item?.IsSoldable == true && session.Character.MaxMateCount < 90 || session.Character.MaxMateCount < 30)
                        {
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.Character.MaxMateCount += 10;
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GET_PET_PLACES"), 10));
                            session.SendPacket(session.Character.GenerateScpStc());
                        }

                        break;

                    // Permanent Backpack Expansion
                    case 601:
                        if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                        {
                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddYears(15),
                                StaticBonusType = StaticBonusType.BackPack
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateExts());
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }

                        break;

                    // Backpack Expansion
                    case 1009:
                        if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                        {
                            session.Character.StaticBonusList.Add(new StaticBonusDTO
                            {
                                CharacterId = session.Character.CharacterId,
                                DateEnd = DateTime.Now.AddDays(EffectValue),
                                StaticBonusType = StaticBonusType.BackPack
                            });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(session.Character.GenerateExts());
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                        }

                        break;

                    // Sealed Tarot Card
                    case 1005:
                        session.Character.GiftAdd((short)(VNum - Effect), 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    // Tarot Card Game
                    case 1894:
                        if (EffectValue == 0)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                session.Character.GiftAdd((short)(Effect + ServerManager.RandomNumber(0, 10)), 1);
                            }

                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }

                        break;

                    // Sealed Tarot Card
                    case 2152:
                        session.Character.GiftAdd((short)(VNum + Effect), 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;

                    // Transformation scrolls
                    case 1001:
                        if (session.Character.IsMorphed)
                        {
                            session.Character.IsMorphed = false;
                            session.Character.Morph = 0;
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                        }
                        else if (!session.Character.UseSp && !session.Character.IsVehicled)
                        {
                            if (Option == 0)
                            {
                                session.Character.LastDelay = DateTime.Now;
                                session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 3,
                                    $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^1"));
                            }
                            else
                            {
                                int[] possibleTransforms = null;

                                switch (EffectValue)
                                {
                                    case 1: // Halloween
                                        possibleTransforms = new[]
                                        {
                                        404, //Torturador pellizcador
                                        405, //Torturador enrollador
                                        406, //Torturador de acero
                                        446, //Guerrero yak
                                        447, //Mago yak
                                        441, //Guerrero de la muerte
                                        276, //Rey polvareda
                                        324, //Princesa Catrisha
                                        248, //Bruja oscura
                                        249, //Bruja de sangre
                                        438, //Bruja blanca fuerte
                                        236, //Guerrero esqueleto
                                        245, //Sombra nocturna
                                        439, //Guerrero esqueleto resucitado
                                        272, //Arquero calavera
                                        274, //Guerrero calavera
                                        2691 //Frankenstein
                                    };
                                        break;

                                    case 2: // Ice Costume
                                        break;

                                    case 3: // Bushtail Costume
                                        break;
                                }

                                if (possibleTransforms != null)
                                {
                                    session.Character.IsMorphed = true;
                                    session.Character.Morph =
                                        1000 + possibleTransforms[ServerManager.RandomNumber(0, possibleTransforms.Length)];
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                    if (VNum != 1914)
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                }
                            }
                        }

                        break;


                    default:
                        switch (EffectValue)
                        {
                            // Angel Base Flag
                            case 965:

                            // Demon Base Flag
                            case 966:
                                if (ServerManager.Instance.ChannelId == 51 &&
                                    session.CurrentMapInstance?.Map.MapId != 130 &&
                                    session.CurrentMapInstance?.Map.MapId != 131 &&
                                    EffectValue - 964 == (short)session.Character.Faction)
                                {
                                    if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.Act4Berios || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.Act4Calvina ||
                                        session.CurrentMapInstance?.MapInstanceType == MapInstanceType.Act4Morcos || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.Act4Hatus ||
                                        session.CurrentMapInstance?.MapInstanceType == MapInstanceType.CaligorInstance)

                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 0));
                                        return;
                                    }

                                    session.CurrentMapInstance?.SummonMonster(new MonsterToSummon((short)EffectValue, new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY },
                                    null, false, isHostile: false, aliveTime: 1800));

                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }

                                break;

                            default:
                                switch (VNum)
                                {
                                    case 5856: // Partner Slot Expansion
                                    case 9113: // Partner Slot Expansion (Limited)
                                        {
                                            if (Option == 0)
                                            {
                                                session.SendPacket(
                                                    $"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2 {Language.Instance.GetMessageFromKey("ASK_PARTNER_MAX")}");
                                            }
                                            else if (session.Character.MaxPartnerCount < 12)
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                session.Character.MaxPartnerCount++;
                                                session.SendPacket(session.Character.GenerateSay(
                                                    Language.Instance.GetMessageFromKey("GET_PARTNER_PLACES"), 10));
                                                session.SendPacket(session.Character.GenerateScpStc());
                                            }
                                        }
                                        break;

                                    case 5931: // Partner Skill Ticket (Single)
                                    case 9109: //Partner Skill Ticket (Limited)
                                        {
                                            if (session?.Character?.Mates == null)
                                            {
                                                return;
                                            }
                                            if (packetsplit == null)
                                            {
                                                // Packet Hacking
                                                return;
                                            }
                                            if (packetsplit.Length < 9)
                                            {
                                                // Packet hacking
                                                return;
                                            }
                                            if (packetsplit.Length != 10 || !byte.TryParse(packetsplit[8], out var petId) ||
                                                !byte.TryParse(packetsplit[9], out var castId))
                                            {
                                                return;
                                            }

                                            if (castId < 0 || castId > 2)
                                            {
                                                return;
                                            }

                                            var partner = session.Character.Mates.ToList().FirstOrDefault(s =>
                                                                          s.IsTeamMember && s.MateType == MateType.Partner && s.PetId == petId);

                                            if (partner?.Sp == null || partner.IsUsingSp)
                                            {
                                                return;
                                            }

                                            var skill = partner.Sp.GetSkill(castId);

                                            if (skill == null)
                                            {
                                                return;
                                            }

                                            if (skill.Level == (byte)PartnerSkillLevelType.S)
                                            {
                                                return;
                                            }

                                            if (partner.Sp.RemoveSkill(castId))
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                                partner.Sp.ReloadSkills();
                                                partner.Sp.FullXp();

                                                session.SendPacket(UserInterfaceHelper.GenerateModal(
                                                    Language.Instance.GetMessageFromKey("PSP_SKILL_RESETTED"), 1));
                                            }

                                            session.SendPacket(partner.GenerateScPacket());
                                        }
                                        break;

                                    case 5932: // Partner Skill Ticket (All)
                                    case 9110: // Partner Skill Ticket (Limited)
                                        {
                                            if (packetsplit == null)
                                            {
                                                // Packet Hacking
                                                return;
                                            }
                                            if (packetsplit.Length < 9)
                                            {
                                                // Packet hacking
                                                return;
                                            }
                                            if (packetsplit.Length != 10 || session?.Character?.Mates == null)
                                            {
                                                return;
                                            }

                                            if (!byte.TryParse(packetsplit[8], out var petId) || !byte.TryParse(packetsplit[9], out var castId))
                                            {
                                                return;
                                            }

                                            if (castId < 0 || castId > 2)
                                            {
                                                return;
                                            }

                                            var partner = session.Character.Mates.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.PetId == petId);

                                            if (partner?.Sp == null || partner.IsUsingSp)
                                            {
                                                return;
                                            }

                                            if (partner.Sp.GetSkillsCount() < 1)
                                            {
                                                return;
                                            }

                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            partner.Sp.ClearSkills();
                                            partner.Sp.FullXp();
                                            session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("PSP_ALL_SKILLS_RESETTED"), 1));
                                            session.SendPacket(partner.GenerateScPacket());
                                        }
                                        break;

                                    #region Flower Quest
                                    case 1087:
                                        if (ServerManager.Instance.ChannelId == 51)
                                        {
                                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 0));
                                            return;
                                        }
                                        if (ServerManager.Instance.FlowerQuestId != null)
                                        {
                                            session.Character.AddQuest((long)ServerManager.Instance.FlowerQuestId);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;
                                    #endregion

                                    // Event Upgrade Scrolls
                                    case 5107:
                                    case 5207:
                                    case 5519:
                                        if (EffectValue != 0)
                                        {
                                            if (session.Character.IsSitting)
                                            {
                                                session.Character.IsSitting = false;
                                                session.SendPacket(session.Character.GenerateRest());
                                            }

                                            session.SendPacket(UserInterfaceHelper.GenerateGuri(12, 1,
                                                session.Character.CharacterId, EffectValue));
                                        }
                                        break;

                                    case 1254:
                                        {
                                            if (!session.Character.Buff.ContainsKey(146))
                                            {
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                                            }
                                            else
                                            {
                                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));

                                            }
                                        }
                                        break;

                                    #region Costume sets
                                    // Rottweiler costume box set (Perm)
                                    case 1518:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(787, 1);
                                            session.Character.GiftAdd(839, 1);
                                        }
                                        break;

                                    // Cat siamois costume box set (Perm)
                                    case 1519:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(790, 1);
                                            session.Character.GiftAdd(842, 1);
                                        }
                                        break;

                                    // Korat costume box set (Perm)
                                    case 1526:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(821, 1);
                                            session.Character.GiftAdd(869, 1);
                                        }
                                        break;

                                    // Black lion costume box set (Perm)
                                    case 1527:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(833, 1);
                                            session.Character.GiftAdd(881, 1);
                                        }
                                        break;

                                    case 5106:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(8153, 1);
                                            session.Character.GiftAdd(8154, 1);
                                            session.Character.GiftAdd(8155, 1);
                                            session.Character.GiftAdd(8156, 1);
                                            session.Character.GiftAdd(8512, 1);
                                            session.Character.GiftAdd(9041, 99);
                                            session.Character.GiftAdd(9043, 99);
                                            session.Character.GiftAdd(9074, 99);
                                            session.Character.GiftAdd(9129, 999);
                                            session.Character.GiftAdd(9143, 1);
                                            session.Character.GiftAdd(9115, 1);
                                        }
                                        break;

                                    // Gold lion costume box set (Perm)
                                    case 1528:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(830, 1);
                                            session.Character.GiftAdd(878, 1);
                                        }
                                        break;

                                    case 5051: // Aqua Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4064, 1);
                                            session.Character.GiftAdd(4065, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5080: // Christmas Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4074, 1);
                                            session.Character.GiftAdd(4077, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5183: // Black Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4107, 1);
                                            session.Character.GiftAdd(4114, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5184: // Blue Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4108, 1);
                                            session.Character.GiftAdd(4115, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5185: // Green Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4109, 1);
                                            session.Character.GiftAdd(4116, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5186: // Red Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4110, 1);
                                            session.Character.GiftAdd(4117, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5187: // Pink Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4111, 1);
                                            session.Character.GiftAdd(4118, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5188: // Light blue Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4112, 1);
                                            session.Character.GiftAdd(4119, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5189: // Yellow Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(4113, 1);
                                            session.Character.GiftAdd(4120, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5190: // Classic Bushtail Costume Set
                                        {
                                            session.Character.GiftAdd(970, 1);
                                            session.Character.GiftAdd(972, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5302: // Fox Oto Costume Set
                                        {
                                            session.Character.GiftAdd(4177, 1);
                                            session.Character.GiftAdd(4179, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    // Magic light costume set box
                                    case 5358:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4185, 1);
                                            session.Character.GiftAdd(4181, 1);
                                        }
                                        break;

                                    //  Magic Dark costume set box
                                    case 5359:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4187, 1);
                                            session.Character.GiftAdd(4183, 1);
                                        }
                                        break;

                                    // Desert costume set box
                                    case 5638:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4317, 1);
                                            session.Character.GiftAdd(4321, 1);
                                        }
                                        break;

                                    // Dancing costume set box
                                    case 5639:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4319, 1);
                                            session.Character.GiftAdd(4323, 1);
                                        }
                                        break;

                                    // Policeman costume set box
                                    case 5599:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4283, 1);
                                            session.Character.GiftAdd(4285, 1);
                                        }
                                        break;

                                    // Nutcracker costume set box
                                    case 5878:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4827, 1);
                                            session.Character.GiftAdd(4829, 1);
                                        }
                                        break;

                                    case 5733: // Easter Rabbit Costume Set
                                        {
                                            session.Character.GiftAdd(4429, 1);
                                            session.Character.GiftAdd(4433, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    case 5572: // Illusionist Costume Set
                                        {
                                            session.Character.GiftAdd(4258, 1);
                                            session.Character.GiftAdd(4260, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                        }

                                    // Football costume pack permanant
                                    case 5441:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(4195, 1);
                                            session.Character.GiftAdd(4196, 1);
                                        }
                                        break;

                                    case 5266: // Bunny (f)
                                        {
                                            session.Character.GiftAdd(4142, 1);
                                            session.Character.GiftAdd(4150, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;


                                    case 5737: // FAIRY COSTUME SET
                                        {
                                            session.Character.GiftAdd(4439, 1);
                                            session.Character.GiftAdd(4441, 1);
                                            session.Character.GiftAdd(4443, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5716: // FIRE DEVIL SET
                                        {
                                            session.Character.GiftAdd(4409, 1);
                                            session.Character.GiftAdd(4411, 1);
                                            session.Character.GiftAdd(4435, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5816: // ICE WITCH
                                        {
                                            session.Character.GiftAdd(4534, 1);
                                            session.Character.GiftAdd(4536, 1);
                                            session.Character.GiftAdd(4538, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5487: // Whit Tiger set
                                        {
                                            session.Character.GiftAdd(4248, 1);
                                            session.Character.GiftAdd(4256, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5486: // Tiger peluche set
                                        {
                                            session.Character.GiftAdd(4252, 1);
                                            session.Character.GiftAdd(4244, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5610: // Viking
                                        {
                                            session.Character.GiftAdd(4301, 1);
                                            session.Character.GiftAdd(4303, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5412: // Party Set 1
                                        {
                                            session.Character.GiftAdd(4219, 1);
                                            session.Character.GiftAdd(4225, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5413: // Party Set 2
                                        {
                                            session.Character.GiftAdd(4220, 1);
                                            session.Character.GiftAdd(4226, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5414: // Party Set 3
                                        {
                                            session.Character.GiftAdd(4221, 1);
                                            session.Character.GiftAdd(4227, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5604: // Portiere
                                        {
                                            session.Character.GiftAdd(4289, 1);
                                            session.Character.GiftAdd(4287, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5789: // Tropical Set
                                        {
                                            session.Character.GiftAdd(4529, 1);
                                            session.Character.GiftAdd(4527, 1);
                                            session.Character.GiftAdd(4531, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 1480: // haloween female
                                        {
                                            session.Character.GiftAdd(4388, 1);
                                            session.Character.GiftAdd(4386, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 1481: // haloween male
                                        {
                                            session.Character.GiftAdd(4392, 1);
                                            session.Character.GiftAdd(4390, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5729: // BOX 3
                                        {
                                            session.Character.GiftAdd(4377, 1);
                                            session.Character.GiftAdd(4375, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5736: // BOX 2
                                        {
                                            session.Character.GiftAdd(4367, 1);
                                            session.Character.GiftAdd(4365, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5742: // BOX 5
                                        {
                                            session.Character.GiftAdd(4073, 1);
                                            session.Character.GiftAdd(4074, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5732:
                                        {
                                            session.Character.GiftAdd(4421, 1);
                                            session.Character.GiftAdd(4425, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;


                                    case 5265: // Bunny (m)
                                        {
                                            session.Character.GiftAdd(4138, 1);
                                            session.Character.GiftAdd(4146, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5723: // Jaguar Set (Vehicle + Costume)
                                        {
                                            session.Character.GiftAdd(4382, 1);
                                            session.Character.GiftAdd(4384, 1);
                                            session.Character.GiftAdd(5834, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;
                                    #endregion

                                    // Martial Artist Starter Pack
                                    case 5826:
                                        {
                                            // Steel Fist
                                            session.Character.GiftAdd(4756, 1, 6, 7);

                                            // Token
                                            session.Character.GiftAdd(4758, 1, 6, 7);

                                            // Trainee Martial Artist's Uniform
                                            session.Character.GiftAdd(4757, 1, 6, 7);

                                            session.Character.GiftAdd(4503, 1);
                                            session.Character.GiftAdd(4504, 1);

                                            for (short itemVNum = 800; itemVNum <= 803; itemVNum++)
                                            {
                                                session.Character.GiftAdd(itemVNum, 1);
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5018: // Weeding box
                                        {
                                            session.Character.GiftAdd(4416, 1); // SP
                                            session.Character.GiftAdd(4416, 1); // SP
                                            session.Character.GiftAdd(1984, 10);
                                            session.Character.GiftAdd(1985, 10);
                                            session.Character.GiftAdd(1986, 10);
                                            session.Character.GiftAdd(1981, 1); // Cupid's arrow
                                            session.Character.GiftAdd(982, 1);
                                            session.Character.GiftAdd(982, 1);
                                            session.Character.GiftAdd(986, 1);
                                            session.Character.GiftAdd(986, 1);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    //Sellaim, Woondine, Eperial, Turik random boxes
                                    case 5004:
                                    case 5005:
                                    case 5006:
                                    case 5007:
                                        int rnd100 = ServerManager.RandomNumber(0, 100);
                                        Item Item = inv.Item;
                                        short[] vnums100 = null;
                                        if (rnd100 < 15 && Item.VNum == 5004)
                                        {
                                            vnums100 = new short[] { 274, 1218 };
                                            byte[] counts = { 1, 1, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums100[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd100 < 15 && Item.VNum == 5005)
                                        {
                                            vnums100 = new short[] { 275, 1218 };
                                            byte[] counts = { 1, 1, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums100[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd100 < 15 && Item.VNum == 5006)
                                        {
                                            vnums100 = new short[] { 276, 1218 };
                                            byte[] counts = { 1, 1, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums100[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd100 < 15 && Item.VNum == 5007)
                                        {
                                            vnums100 = new short[] { 277, 1218 };
                                            byte[] counts = { 1, 1, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums100[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else
                                        {
                                            short[] vnums2 = null;
                                            vnums2 = new short[] { 1904, 1296, 1296, 1122, 2282, 1219, 1119, 2158 };
                                            byte[] counts2 = { 1, 5, 3, 45, 40, 1, 2, 5 };
                                            int item2 = ServerManager.RandomNumber(0, 8);
                                            session.Character.GiftAdd(vnums2[item2], counts2[item2]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    //Great Sellaim, Great Woondine, Great Eperial, Great Turik random boxes
                                    case 5014:
                                    case 5015:
                                    case 5016:
                                    case 5017:
                                        int rnd101 = ServerManager.RandomNumber(0, 100);
                                        Item Item1 = inv.Item;
                                        short[] vnums101 = null;
                                        if (rnd101 < 10 && Item1.VNum == 5014)
                                        {
                                            vnums101 = new short[] { 278, 1218 };
                                            byte[] counts = { 1, 2, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums101[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd101 < 10 && Item1.VNum == 5015)
                                        {
                                            vnums101 = new short[] { 279, 1218 };
                                            byte[] counts = { 1, 2, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums101[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd101 < 10 && Item1.VNum == 5016)
                                        {
                                            vnums101 = new short[] { 280, 1218 };
                                            byte[] counts = { 1, 2, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums101[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else if (rnd101 < 10 && Item1.VNum == 5017)
                                        {
                                            vnums101 = new short[] { 281, 1218 };
                                            byte[] counts = { 1, 2, };
                                            int item = ServerManager.RandomNumber(0, 2);
                                            session.Character.GiftAdd(vnums101[item], counts[item]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        else
                                        {
                                            short[] vnums2 = null;
                                            vnums2 = new short[] { 1904, 1296, 1286, 1122, 2282, 1219, 1119, 2158 };
                                            byte[] counts2 = { 2, 10, 6, 90, 80, 2, 2, 10 };
                                            int item2 = ServerManager.RandomNumber(0, 8);
                                            session.Character.GiftAdd(vnums2[item2], counts2[item2]);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    case 5171: // Ancient's Wizard opo box
                                        {
                                            int rnd102 = ServerManager.RandomNumber(0, 100);
                                            Item Item2 = inv.Item;
                                            short[] vnums102 = null;
                                            if (rnd102 < 10 && Item2.VNum == 5171)
                                            {
                                                vnums102 = new short[] { 397, 4085, 4086, 4087, 4088, 4089, 4090, 4091 };
                                                byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1 };
                                                int item2 = ServerManager.RandomNumber(0, 8);
                                                session.Character.GiftAdd(vnums102[item2], counts[item2]);
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            }
                                        }
                                        break;


                                    #region Mount boxes
                                    // Scooter box
                                    case 1926:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(1906, 1);
                                        }
                                        break;

                                    // Tapis box
                                    case 1927:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(1907, 1);
                                        }
                                        break;

                                    // White tiger box
                                    case 1966:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(1965, 1);
                                        }
                                        break;

                                    // Balai box
                                    case 5153:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5152, 1);
                                        }
                                        break;

                                    // Yakari box
                                    case 5181:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5173, 1);
                                        }
                                        break;

                                    // Mac Umulonimbus box
                                    case 5118:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5117, 1);
                                        }
                                        break;

                                    // Nossi box
                                    case 5197:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5196, 1);
                                        }
                                        break;

                                    // Chameau box
                                    case 5915:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5914, 1);
                                        }
                                        break;

                                    // Rollers box
                                    case 5235:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5234, 1);
                                        }
                                        break;

                                    // VTT box
                                    case 5233:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5232, 1);
                                        }
                                        break;

                                    // Skateboard box
                                    case 5237:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5236, 1);
                                        }
                                        break;

                                    // Skateboard inivisble box
                                    case 5229:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5228, 1);
                                        }
                                        break;

                                    // Snowboard box
                                    case 5241:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5240, 1);
                                        }
                                        break;

                                    // Skis box
                                    case 5239:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5238, 1);
                                        }
                                        break;

                                    // Ski invisible box
                                    case 5227:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5226, 1);
                                        }
                                        break;

                                    // Magic Bone Drake
                                    case 5998:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5997, 1);
                                        }
                                        break;

                                    // Aerosurfeur box
                                    case 5361:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5360, 1);
                                        }
                                        break;

                                    // Jaguar box
                                    case 5835:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5834, 1);
                                        }
                                        break;

                                    // Traineau box
                                    case 5713:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5712, 1);
                                        }
                                        break;
                                    // Ski invisible box
                                    case 5744:
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.GiftAdd(5743, 1);
                                        }
                                        break;
                                    #endregion

                                    case 9678: // Fill glacernon raid bar
                                        {
                                            if (ServerManager.Instance.ChannelId == 51 && ServerManager.Instance.Act4DemonStat.Mode == 0 && ServerManager.Instance.Act4AngelStat.Mode == 0)
                                            {
                                                switch (session.Character.Faction)
                                                {
                                                    case FactionType.Angel:
                                                        {
                                                            ServerManager.Instance.Act4AngelStat.Percentage += 2000; //20%
                                                        }
                                                        break;

                                                    case FactionType.Demon:
                                                        {
                                                            ServerManager.Instance.Act4DemonStat.Percentage += 2000;
                                                        }
                                                        break;

                                                }
                                            }
                                            else
                                            {
                                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 0));
                                                return;
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        }
                                        break;

                                    // Soulstone Blessing
                                    case 1362:
                                    case 5195:
                                    case 5211:
                                    case 9075:
                                        if (!session.Character.Buff.ContainsKey(146))
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                                        }
                                        else
                                        {
                                            session.SendPacket(
                                                UserInterfaceHelper.GenerateMsg(
                                                    Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                                        }

                                        break;

                                    case 1428:
                                        session.SendPacket("guri 18 1");
                                        break;

                                    case 1429:
                                        session.SendPacket("guri 18 0");
                                        break;

                                    case 1904:
                                        short[] items = { 1894, 1895, 1896, 1897, 1898, 1899, 1900, 1901, 1902, 1903 };
                                        for (var i = 0; i < 5; i++)
                                        {
                                            session.Character.GiftAdd(items[ServerManager.RandomNumber(0, items.Length)], 1);

                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    case 5370:
                                        if (session.Character.Buff.Any(s => s.Card.CardId == 393) || session.Character.Buff.Any(s => s.Card.CardId == 4047))
                                        {
                                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ALREADY_GOT_BUFF"), session.Character.Buff.FirstOrDefault(s => s.Card.CardId == 393)?.Card.Name), 10));
                                            return;
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 393 });
                                        break;

                                    case 11117:
                                        if (session.Character.Buff.Any(s => s.Card.CardId == 4047) || session.Character.Buff.Any(s => s.Card.CardId == 393))
                                        {
                                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ALREADY_GOT_BUFF"), session.Character.Buff.FirstOrDefault(s => s.Card.CardId == 4047)?.Card.Name), 10));
                                            return;
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 4047 });
                                        break;

                                    case 5841:
                                        var rnd = ServerManager.RandomNumber(0, 1000);
                                        short[] vnums = null;
                                        if (rnd < 900)
                                        {
                                            vnums = new short[] { 4356, 4357, 4358, 4359 };
                                        }
                                        else
                                        {
                                            vnums = new short[] { 4360, 4361, 4362, 4363 };
                                        }

                                        session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 4)], 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    case 5916:
                                    case 5927:
                                        session.Character.AddStaticBuff(new StaticBuffDTO
                                        {
                                            CardId = 340,
                                            CharacterId = session.Character.CharacterId,
                                            RemainingTime = 7200
                                        });
                                        session.Character.RemoveBuff(339, true);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    //Ice Oil
                                    case 5929:
                                    case 5930:
                                        session.Character.AddStaticBuff(new StaticBuffDTO
                                        {
                                            CardId = 340,
                                            CharacterId = session.Character.CharacterId,
                                            RemainingTime = 600
                                        });
                                        session.Character.RemoveBuff(339, true);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    //Burning Cold
                                    case 7331:
                                        session.Character.AddStaticBuff(new StaticBuffDTO
                                        {
                                            CardId = 1145,
                                            CharacterId = session.Character.CharacterId,
                                            RemainingTime = 600
                                        });
                                        session.Character.RemoveBuff(1144, true);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    // Mother Nature's Rune Pack (limited)
                                    case 9117:
                                        rnd = ServerManager.RandomNumber(0, 1000);
                                        vnums = null;
                                        if (rnd < 900)
                                        {
                                            vnums = new short[] { 8312, 8313, 8314, 8315 };
                                        }
                                        else
                                        {
                                            vnums = new short[] { 8316, 8317, 8318, 8319 };
                                        }

                                        session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 4)], 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;

                                    default:
                                        break;
                                }

                                break;
                        }

                        break;
                }

                session.Character.IncrementQuests(QuestType.Use, inv.ItemVNum);
            }
            catch (Exception ex) {
                MessageExtension.SendRed(session, "An Error occured, please report this  to an Admin");
                Logger.Warn(ex.ToString());
            }
        }



        #endregion
    }
}