using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class GetGiftPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GetGiftPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GetGift(GetGiftPacket getGiftPacket)
        {
            var giftId = getGiftPacket.GiftId;
            if (Session.Character.MailList.ContainsKey(giftId))
            {
                var mail = Session.Character.MailList[giftId];
                if (getGiftPacket.Type == 4 && mail.AttachmentVNum != null)
                {
                    if (Session.Character.Inventory.CanAddItem((short)mail.AttachmentVNum))
                    {
                        var newInv = Session.Character.Inventory.AddNewToInventory((short)mail.AttachmentVNum, mail.AttachmentAmount, Upgrade: mail.AttachmentUpgrade, Rare: (sbyte)mail.AttachmentRarity, Design: mail.AttachmentDesign)
                            .FirstOrDefault();
                        if (newInv != null)
                        {
                            if (newInv.Rare != 0) newInv.SetRarityPoint();

                            if (newInv.Item.EquipmentSlot == EquipmentType.Gloves || newInv.Item.EquipmentSlot == EquipmentType.Boots)
                            {
                                newInv.DarkResistance = (short)(newInv.Item.DarkResistance * newInv.Upgrade);
                                newInv.LightResistance = (short)(newInv.Item.LightResistance * newInv.Upgrade);
                                newInv.WaterResistance = (short)(newInv.Item.WaterResistance * newInv.Upgrade);
                                newInv.FireResistance = (short)(newInv.Item.FireResistance * newInv.Upgrade);
                            }

                            Logger.LogUserEvent("PARCEL_GET", Session.GenerateIdentity(), $"IIId: {newInv.Id} ItemVNum: {newInv.ItemVNum} Amount: {mail.AttachmentAmount} Sender: {mail.SenderId}");

                            Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"), newInv.Item.Name, mail.AttachmentAmount), 12));
                            DAOFactory.MailDAO.DeleteById(mail.MailId);
                            Session.SendPacket($"parcel 2 1 {giftId}");
                            Session.Character.MailList.Remove(giftId);
                        }
                    }
                    else
                    {
                        Session.SendPacket("parcel 5 1 0");
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                    }
                }
                else if (getGiftPacket.Type == 5)
                {
                    Session.SendPacket($"parcel 7 1 {giftId}");

                    if (DAOFactory.MailDAO.LoadById(mail.MailId) != null)
                    {
                        DAOFactory.MailDAO.DeleteById(mail.MailId);
                    }

                    if (Session.Character.MailList.ContainsKey(giftId))
                    {
                        Session.Character.MailList.Remove(giftId);
                    }
                }
            }
        }

        #endregion
    }
}