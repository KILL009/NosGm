using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Handler.PacketHandler.Basic
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
            if (getGiftPacket == null || Session?.Character?.MailList == null)
            {
                return;
            }

            // Serializes parcel mutations for this character. This closes the double-click window
            // while the deterministic ItemInstanceId closes the crash/restart window.
            lock (Session.Character.MailList)
            {
                var giftId = getGiftPacket.GiftId;
                if (!Session.Character.MailList.TryGetValue(giftId, out var mail))
                {
                    return;
                }

                if (getGiftPacket.Type == 4 && mail.AttachmentVNum.HasValue)
                {
                    ClaimAttachment(giftId, mail);
                }
                else if (getGiftPacket.Type == 5)
                {
                    DeleteParcel(giftId, mail.MailId);
                }
            }
        }

        private void ClaimAttachment(int giftId, NosGm.Data.MailDTO mail)
        {
            var itemVNum = (short)mail.AttachmentVNum.Value;
            var itemDefinition = ServerManager.GetItem(itemVNum);
            if (itemDefinition == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("Invalid parcel item. Please contact a GM.", 0));
                return;
            }

            var itemInstanceId = CreateDeterministicGuid("NosGM.MailClaim.Item", mail.MailId);
            var traceOperationId = CreateDeterministicGuid("NosGM.MailClaim.Trace", mail.MailId);
            var inventory = Session.Character.Inventory;
            var itemInstance = inventory.GetItemInstanceById(itemInstanceId);
            var newlyCreated = false;

            if (itemInstance == null)
            {
                var persisted = DAOFactory.ItemInstanceDAO.LoadById(itemInstanceId);
                if (persisted != null)
                {
                    itemInstance = new ItemInstance(persisted);
                    var occupied = inventory.LoadBySlotAndType(itemInstance.Slot, itemInstance.Type);
                    if (occupied == null)
                    {
                        if (inventory.AddToInventoryWithSlotAndType(itemInstance, itemInstance.Type, itemInstance.Slot) == null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                "The saved parcel item could not be restored. Relog and try again.", 0));
                            return;
                        }
                    }
                    else if (occupied.Id != itemInstance.Id)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            "The parcel was already saved, but its slot is busy. Relog to refresh the inventory.", 0));
                        return;
                    }
                }
                else
                {
                    itemInstance = NosGm.GameObject.Inventory.InstantiateItemInstance(
                        itemVNum,
                        Session.Character.CharacterId,
                        mail.AttachmentAmount > 0 ? mail.AttachmentAmount : (short)1);
                    itemInstance.Id = itemInstanceId;
                    itemInstance.Rare = unchecked((sbyte)mail.AttachmentRarity);
                    itemInstance.Upgrade = mail.AttachmentUpgrade;
                    itemInstance.Design = mail.AttachmentDesign;

                    ApplyEquipmentValues(itemInstance);

                    var freeSlot = inventory.getFreeSlot(itemInstance.Type);
                    if (!freeSlot.HasValue)
                    {
                        Session.SendPacket("parcel 5 1 0");
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                        return;
                    }

                    if (inventory.AddToInventoryWithSlotAndType(itemInstance, itemInstance.Type, freeSlot.Value) == null)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                        return;
                    }

                    if (DAOFactory.ItemInstanceDAO.InsertOrUpdate(itemInstance) == null)
                    {
                        inventory.DeleteById(itemInstance.Id);
                        Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(
                            itemInstance.Type, itemInstance.Slot));
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            "The parcel could not be saved. Nothing was consumed; please try again.", 0));
                        return;
                    }

                    newlyCreated = true;
                }
            }

            var traceSource = mail.DeliverySource == ItemTraceSource.Unknown
                ? ItemTraceSource.Mail
                : mail.DeliverySource;
            ItemTraceService.Instance.Record(
                traceOperationId,
                0,
                ItemTraceAction.Created,
                traceSource,
                null,
                itemInstance,
                actorCharacterId: Session.Character.CharacterId,
                actorName: Session.Character.Name,
                reason: "Parcel attachment claimed",
                metadata: new
                {
                    mail.MailId,
                    mail.DeliveryOperationId,
                    mail.Title,
                    RecoveredExistingItem = !newlyCreated
                });

            DAOFactory.MailDAO.MarkDeliveryClaimed(mail.MailId, itemInstance.Id);

            var persistedMail = DAOFactory.MailDAO.LoadById(mail.MailId);
            if (persistedMail != null && DAOFactory.MailDAO.DeleteById(mail.MailId) == DeleteResult.Error)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    "The item was saved, but the parcel cleanup failed. Claim it again to finish safely.", 0));
                return;
            }

            Session.SendPacket($"parcel 2 1 {giftId}");
            Session.Character.MailList.Remove(giftId);

            Logger.LogUserEvent("PARCEL_GET", Session.GenerateIdentity(),
                $"IIId: {itemInstance.Id} ItemVNum: {itemInstance.ItemVNum} Amount: {itemInstance.Amount} Sender: {mail.SenderId} Operation: {traceOperationId}");
            Session.SendPacket(Session.Character.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"),
                    itemInstance.Item.Name, mail.AttachmentAmount), 12));
        }

        private static void ApplyEquipmentValues(ItemInstance itemInstance)
        {
            if (itemInstance.Rare != 0)
            {
                itemInstance.SetRarityPoint();
            }

            if (itemInstance.Item.EquipmentSlot == EquipmentType.Gloves ||
                itemInstance.Item.EquipmentSlot == EquipmentType.Boots)
            {
                itemInstance.DarkResistance = (short)(itemInstance.Item.DarkResistance * itemInstance.Upgrade);
                itemInstance.LightResistance = (short)(itemInstance.Item.LightResistance * itemInstance.Upgrade);
                itemInstance.WaterResistance = (short)(itemInstance.Item.WaterResistance * itemInstance.Upgrade);
                itemInstance.FireResistance = (short)(itemInstance.Item.FireResistance * itemInstance.Upgrade);
            }
        }

        private void DeleteParcel(int giftId, long mailId)
        {
            Session.SendPacket($"parcel 7 1 {giftId}");

            var persisted = DAOFactory.MailDAO.LoadById(mailId);
            if (persisted != null && DAOFactory.MailDAO.DeleteById(mailId) == DeleteResult.Error)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    "The parcel could not be deleted. Please try again.", 0));
                return;
            }

            Session.Character.MailList.Remove(giftId);
        }

        private static Guid CreateDeterministicGuid(string scope, long value)
        {
            using (var sha256 = SHA256.Create())
            {
                var payload = Encoding.UTF8.GetBytes(
                    scope + "|" + value.ToString(CultureInfo.InvariantCulture));
                var hash = sha256.ComputeHash(payload);
                var guidBytes = hash.Take(16).ToArray();
                return new Guid(guidBytes);
            }
        }

        #endregion
    }
}
