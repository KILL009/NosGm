using NosGm.Configuration;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Dynamic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Npc
{
    public class SellPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SellPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SellShop(SellPacket sellPacket)
        {
            if (Session.Character.ExchangeInfo?.ExchangeList.Count > 0 || Session.Character.IsShopping)
            {
                return;
            }

            if (sellPacket.Amount.HasValue && sellPacket.Slot.HasValue && sellPacket.Amount.Value > 0)
            {
                var type = (InventoryType)sellPacket.Data;

                if (type == InventoryType.Bazaar)
                {
                    return;
                }

                short amount = sellPacket.Amount.Value;
                byte slot = sellPacket.Slot.Value;

                if (amount < 1)
                {
                    return;
                }

                var inv = Session.Character.Inventory.LoadBySlotAndType(slot, type);

                if (inv == null || amount > inv.Amount)
                {
                    return;
                }

                if (Session.Character.MinilandObjects.Any(s => s.ItemInstanceId == inv.Id))
                {
                    return;
                }

                if (!inv.Item.IsSoldable)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateShopMemo(2,
                        string.Format(Language.Instance.GetMessageFromKey("ITEM_NOT_SOLDABLE"))));
                    return;
                }

                if (inv.Item.VNum == 1046)
                {
                    Session.Disconnect();
                    return;
                }

                long price = inv.Item.SellToNpcPrice;

                if (price < 1)
                {
                    price = 1;
                }

                if (Session.Character.Gold + price * amount > GameConfiguration.MaxGold)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"),
                        0));
                    return;
                }

                Session.Character.Gold += price * amount;
                Session.SendPacket(UserInterfaceHelper.GenerateShopMemo(1,
                    string.Format(Language.Instance.GetMessageFromKey("SELL_ITEM_VALID"), inv.Item.Name, amount)));

                Session.Character.Inventory.RemoveItemFromInventory(inv.Id, amount);
                Session.SendPacket(Session.Character.GenerateGold());
            }
            else
            {
                short vnum = sellPacket.Data;

                var skill = Session.Character.Skills[vnum];

                if (skill == null)
                {
                    return;
                }

                if (vnum == 200 + 20 * (byte)Session.Character.Class)
                {
                    return;
                }

                if (vnum == 201 + 20 * (byte)Session.Character.Class)
                {
                    return;
                }

                Session.Character.Gold += skill.Skill.Price;
                Session.SendPacket(Session.Character.GenerateGold());

                foreach (var loadedSkill in Session.Character.Skills.GetAllItems())
                {
                    if (skill.Skill.SkillVNum != loadedSkill.Skill.UpgradeSkill)
                    {
                        continue;
                    }

                    Session.Character.Skills.Remove(loadedSkill.SkillVNum);
                }

                Session.Character.Skills.Remove(skill.SkillVNum);
                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPackets(Session.Character.GenerateQuicklist());
                Session.SendPacket(Session.Character.GenerateLev());
            }
        }

        #endregion
    }
}