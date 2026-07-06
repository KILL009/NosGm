using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class MviPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MviPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MoveItem(MviPacket mviPacket)
        {
            if (mviPacket != null)
            {

                if (mviPacket.InventoryType != InventoryType.Equipment
                    && mviPacket.InventoryType != InventoryType.Main
                    && mviPacket.InventoryType != InventoryType.Etc
                    && mviPacket.InventoryType != InventoryType.Miniland)
                {
                    return;
                }

                if (mviPacket.Amount <= 0)
                {
                    Session.SendPacket($"say 1 0 10 Fixxed, thanks for try.");
                    return;
                }

                if (mviPacket.Amount < 1)
                {
                    return;
                }

                if (mviPacket.Slot == mviPacket.DestinationSlot)
                {
                    return;
                }

                if (mviPacket.InventoryType == InventoryType.Wear)
                {
                    return;
                }

                lock (Session.Character.Inventory)
                {
                    // check if the destination slot is out of range
                    if (mviPacket.DestinationSlot > 48 + (Session.Character.HaveBackpack() ? 1 : 0) * 12 +
                        (Session.Character.HaveExtension() ? 1 : 0) * 60)
                    {
                        return;
                    }

                    if (mviPacket.InventoryType == InventoryType.Miniland)
                    {
                        ItemInstance minigame1 = Session.Character.Inventory.LoadBySlotAndType(mviPacket.Slot, InventoryType.Miniland);

                        if (minigame1 != null)
                        {
                            MinilandObject minilandObject1 = Session.Character.MinilandObjects.Find(i => i.ItemInstanceId == minigame1.Id);

                            if (minilandObject1 != null)
                            {
                                return;
                            }
                        }

                        ItemInstance minigame2 = Session.Character.Inventory.LoadBySlotAndType(mviPacket.DestinationSlot, InventoryType.Miniland);

                        if (minigame2 != null)
                        {
                            MinilandObject minilandObject2 = Session.Character.MinilandObjects.Find(i => i.ItemInstanceId == minigame2.Id);

                            if (minilandObject2 != null)
                            {
                                return;
                            }
                        }
                    }

                    if (mviPacket.InventoryType == InventoryType.Equipment && mviPacket.Amount > 1)
                    {
                        ItemInstance item = Session.Character.Inventory.LoadBySlotAndType(mviPacket.Slot, mviPacket.InventoryType);
                        return;
                    }

                    // check if the character is allowed to move the item
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }

                    // actually move the item from source to destination
                    Session.Character.Inventory.MoveItem(mviPacket.InventoryType, mviPacket.InventoryType,
                        mviPacket.Slot, mviPacket.Amount, mviPacket.DestinationSlot, out var previousInventory,
                        out var newInventory);
                    if (newInventory == null)
                    {
                        return;
                    }

                    Session.SendPacket(newInventory.GenerateInventoryAdd());

                    Session.SendPacket(previousInventory != null
                        ? previousInventory.GenerateInventoryAdd()
                        : UserInterfaceHelper.Instance.GenerateInventoryRemove(mviPacket.InventoryType,
                            mviPacket.Slot));
                }
            }
        }

        #endregion
    }
}