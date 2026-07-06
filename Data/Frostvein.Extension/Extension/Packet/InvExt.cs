

using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;


namespace Frostvein.Extension.Extension.Packet
{
    public static class InvExt
    {
        public static void CloseExchange(this ClientSession session, ClientSession targetSession)
        {
            if (targetSession?.Character.ExchangeInfo != null)
            {
                targetSession.SendPacket("exc_close 0");
                targetSession.Character.ExchangeInfo = null;
                targetSession.Character.TradeRequests.Clear();
            }

            if (session?.Character.ExchangeInfo != null)
            {
                session.SendPacket("exc_close 0");
                session.Character.ExchangeInfo = null;
                session.Character.TradeRequests.Clear();
            }
        }

        public static void Exchange(this ClientSession sourceSession, ClientSession targetSession)
        {
            if (sourceSession?.Character.ExchangeInfo == null)
            {
                return;
            }

            var data = "";

            // remove all items from source session
            foreach (ItemInstance item in sourceSession.Character.ExchangeInfo.ExchangeList)
            {
                var invtemp = sourceSession.Character.Inventory.GetItemInstanceById(item.Id);
                if (invtemp != null && invtemp?.Amount >= item.Amount)
                {
                    sourceSession.Character.Inventory.RemoveItemFromInventory(invtemp.Id, item.Amount);
                }
                else
                {
                    return;
                }                  
            }

            // add all items to target session
            foreach (var item in sourceSession.Character.ExchangeInfo.ExchangeList)
            {
                var item2 = item.DeepCopy();
                item2.Id = Guid.NewGuid();
                data +=
                    $"[OldIIId: {item.Id} NewIIId: {item2.Id} ItemVNum: {item.ItemVNum} Amount: {item.Amount} Rare: {item.Rare} Upgrade: {item.Upgrade}]";
                var inv = targetSession.Character.Inventory.AddToInventory(item2);
                if (inv.Count == 0)
                {
                    // do what?
                }
            }

            data += $"[Gold: {sourceSession.Character.ExchangeInfo.Gold}]";
            data += $"[BankGold: {sourceSession.Character.ExchangeInfo.BankGold}]";

            // handle gold
            sourceSession.Character.Gold -= sourceSession.Character.ExchangeInfo.Gold;
            sourceSession.Character.GoldBank -= sourceSession.Character.ExchangeInfo.BankGold;
            sourceSession.SendPacket(sourceSession.Character.GenerateGold());
            targetSession.Character.Gold += sourceSession.Character.ExchangeInfo.Gold;
            targetSession.Character.GoldBank += sourceSession.Character.ExchangeInfo.BankGold;
            targetSession.SendPacket(targetSession.Character.GenerateGold());

            // all items and gold from sourceSession have been transferred, clean exchange info
            //LOGGER($"[TRADE] Source: {sourceSession.GenerateIdentity()} | Target: {targetSession.GenerateIdentity()} | Data: {data}");

            sourceSession.Character.ExchangeInfo = null;
        }

        public static void ChangeSp(this ClientSession Session)
        {
            ItemInstance sp =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            ItemInstance fairy =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (sp != null)
            {
                if (Session.Character.GetReputationIco() < sp.Item.ReputationMinimum)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOW_REP"),
                        0));
                    return;
                }

                if (fairy != null && sp.Item.Element != 0 && fairy.Item.Element != sp.Item.Element
                    && fairy.Item.Element != sp.Item.SecondaryElement)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_FAIRY"),
                        0));
                    return;
                }

                if (new int[] { 4494, 4495, 4496 }.Contains(sp.ItemVNum))
                {
                    if (Session.Character.Timespace == null)
                    {
                        return;
                    }
                    else if (ServerManager.Instance.TimeSpaces.Any(s => s.SpNeeded?[(byte)Session.Character.Class] == sp.ItemVNum))
                    {
                        if (Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != sp.ItemVNum)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                Session.Character.FishingSpotsMapId = 1;
                Session.Character.FishingSpotsMapX = ServerManager.RandomNumber<short>(78, 81);
                Session.Character.FishingSpotsMapY = ServerManager.RandomNumber<short>(114, 118);
                Session.Character.ChargeValue = 0;
                Session.Character.DisableBuffs(BuffType.All);
                Session.Character.EquipmentBCards.AddRange(sp.Item.BCards);
                Session.Character.LastTransform = DateTime.Now;
                Session.Character.UseSp = true;
                Session.Character.Morph = sp.Item.Morph;
                Session.Character.MorphUpgrade = sp.Upgrade;
                Session.Character.MorphUpgrade2 = sp.Design;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                Session.SendPacket(Session.Character.GenerateLev());
                Session.CurrentMapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 196),
                    Session.Character.PositionX, Session.Character.PositionY);
                Session.CurrentMapInstance?.Broadcast(
                    UserInterfaceHelper.GenerateGuri(6, 1, Session.Character.CharacterId), Session.Character.PositionX,
                    Session.Character.PositionY);
                Session.SendPacket(Session.Character.GenerateSpPoint());
                Session.Character.LoadSpeed();
                Session.SendPacket(Session.Character.GenerateCond());
                Session.SendPacket(Session.Character.GenerateStat());
                Session.SendPackets(Session.Character.GenerateStatChar());
                CharacterHelper.AddSpecialistWingsBuff(Session);
                Session.Character.SkillsSp = new ThreadSafeSortedList<int, CharacterSkill>();
                Parallel.ForEach(ServerManager.GetAllSkill(), skill =>
                {
                    if (skill.UpgradeType == sp.Item.Morph && skill.SkillType == 1 && sp.SpLevel >= skill.LevelMinimum)
                    {
                        Session.Character.SkillsSp[skill.SkillVNum] = new CharacterSkill
                        {
                            SkillVNum = skill.SkillVNum,
                            CharacterId = Session.Character.CharacterId
                        };
                    }
                });
                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPackets(Session.Character.GenerateQuicklist());
                Session.Character.LoadPartnerSkills(true);
                //LOGGER($"[TRANSFORM] {Session.Character.Name} | Specialist: {sp.Item.Morph} | Name: {sp.Item.Name}");
            }
        }
    }
}