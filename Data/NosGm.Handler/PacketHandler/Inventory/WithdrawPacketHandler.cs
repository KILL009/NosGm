using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class WithdrawPacketHandler : IPacketHandler
    {
        #region Instantiation

        public WithdrawPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Withdraw(WithdrawPacket withdrawPacket)
        {
            Session.SendPacket($"say 1 0 10 Soon will available, now is lock");
            return;
            if (withdrawPacket == null)
            {
                return;
            }

            if (withdrawPacket.Amount < 1)
            {
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (Session.Character.HasShopOpened)
            {
                return;
            }

            if (Session.Character.LastWarehouse > DateTime.Now)
            {
                Session.SendPacket("msg 4 Delay Register, please wait...");
                return;
            }

            ItemInstance previousInventory = Session.Character.Inventory.LoadBySlotAndType(withdrawPacket.Slot, withdrawPacket.PetBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse);

            if (previousInventory == null)
            {
                return;
            }

            if (previousInventory.Item.IsWarehouseable)
            {
                Session.SendPacket("msg 4 You can't do this");
                return;
            }

            if (withdrawPacket.Amount > previousInventory.Amount)
            {
                return;
            }

            if (!Session.Character.Inventory.CanAddItem(previousInventory.ItemVNum))
            {
                return;
            }

            var item2 = previousInventory.DeepCopy();
            item2.Id = Guid.NewGuid();
            item2.Amount = withdrawPacket.Amount;

            Logger.LogUserEvent("STASH_WITHDRAW", Session.GenerateIdentity(),
                $"[Withdraw]OldIIId: {previousInventory.Id} NewIIId: {item2.Id} Amount: {withdrawPacket.Amount} PartnerBackpack: {withdrawPacket.PetBackpack}");
            Logger.LogUserEvent("STASH_WITHDRAW_PACKET", Session.GenerateIdentity(), $"Packet string: {withdrawPacket.OriginalContent.ToString()}");

            Session.Character.Inventory.RemoveItemFromInventory(previousInventory.Id, withdrawPacket.Amount);

            Session.Character.Inventory.AddToInventory(item2, item2.Item.Type);
            Session.Character.Inventory.LoadBySlotAndType(withdrawPacket.Slot, withdrawPacket.PetBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse);

            if (previousInventory.Amount > 0)
            {
                Session.SendPacket(withdrawPacket.PetBackpack
                    ? previousInventory.GeneratePStash()
                    : previousInventory.GenerateStash());
            }
            else
            {
                Session.SendPacket(withdrawPacket.PetBackpack
                    ? UserInterfaceHelper.Instance.GeneratePStashRemove(withdrawPacket.Slot)
                    : UserInterfaceHelper.Instance.GenerateStashRemove(withdrawPacket.Slot));
            }

            Session.Character.LastWarehouse = DateTime.Now.AddSeconds(3);
        }

        #endregion
    }
}