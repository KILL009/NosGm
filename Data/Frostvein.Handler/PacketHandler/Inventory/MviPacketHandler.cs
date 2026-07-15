using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.Packets.Packets.ClientPackets;


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
            if (mviPacket == null ||
                Session?.Character?.Inventory == null ||
                !Session.HasSelectedCharacter ||
                Session.IsDisposing)
            {
                return;
            }

            if (mviPacket.InventoryType != InventoryType.Equipment
                && mviPacket.InventoryType != InventoryType.Main
                && mviPacket.InventoryType != InventoryType.Etc
                && mviPacket.InventoryType != InventoryType.Miniland)
            {
                return;
            }

            if (mviPacket.Amount <= 0 ||
                mviPacket.Slot < 0 ||
                mviPacket.DestinationSlot < 0 ||
                mviPacket.Slot == mviPacket.DestinationSlot ||
                mviPacket.InventoryType == InventoryType.Wear)
            {
                return;
            }

            lock (Session.Character.Inventory)
            {
                int maximumSlot =
                    48
                    + (Session.Character.HaveBackpack() ? 12 : 0)
                    + (Session.Character.HaveExtension() ? 60 : 0);

                if (mviPacket.DestinationSlot >= maximumSlot)
                {
                    return;
                }

                if (Session.Character.InExchangeOrTrade)
                {
                    return;
                }

                ItemInstance sourceItem =
                    Session.Character.Inventory.LoadBySlotAndType(
                        mviPacket.Slot,
                        mviPacket.InventoryType);

                if (sourceItem == null ||
                    sourceItem.Amount < mviPacket.Amount)
                {
                    return;
                }

                if (mviPacket.InventoryType == InventoryType.Equipment &&
                    mviPacket.Amount != 1)
                {
                    return;
                }

                if (mviPacket.InventoryType == InventoryType.Miniland)
                {
                    MinilandObject sourceObject =
                        Session.Character.MinilandObjects.Find(
                            item => item.ItemInstanceId == sourceItem.Id);

                    if (sourceObject != null)
                    {
                        return;
                    }

                    ItemInstance destinationItem =
                        Session.Character.Inventory.LoadBySlotAndType(
                            mviPacket.DestinationSlot,
                            InventoryType.Miniland);

                    if (destinationItem != null)
                    {
                        MinilandObject destinationObject =
                            Session.Character.MinilandObjects.Find(
                                item => item.ItemInstanceId == destinationItem.Id);

                        if (destinationObject != null)
                        {
                            return;
                        }
                    }
                }
                Logger.Info("===== CLIENT MVI HANDLER =====");
                Logger.Info($"PACKET TYPE = {mviPacket.GetType().FullName}");
                Logger.Info($"HANDLER ASSEMBLY = {typeof(MviPacketHandler).Assembly.Location}");
                // actually move the item from source to destination
                Session.Character.Inventory.MoveItem(
                    mviPacket.InventoryType,
                    mviPacket.InventoryType,
                    mviPacket.Slot,
                    mviPacket.Amount,
                    mviPacket.DestinationSlot,
                    out var previousInventory,
                    out var newInventory);

                if (newInventory == null)
                {
                    return;
                }

                string sourcePacket;

                // Cliente moderno: Equipment se vacía usando VNum 0 y seis campos.
                if (mviPacket.InventoryType == InventoryType.Equipment)
                {
                    sourcePacket = previousInventory != null
                        ? previousInventory.GenerateInventoryAdd()
                        : $"ivn 0 {mviPacket.Slot}.0.0.0.0.0";
                }
                else
                {
                    sourcePacket = previousInventory != null
                        ? previousInventory.GenerateInventoryAdd()
                        : UserInterfaceHelper.Instance.GenerateInventoryRemove(
                            mviPacket.InventoryType,
                            mviPacket.Slot);
                }

                string destinationPacket = newInventory.GenerateInventoryAdd();

                Logger.Info("===== CUSTOM MVI ACTIVE =====");
                Logger.Info($"MVI SOURCE: {sourcePacket}");
                Logger.Info($"MVI DESTINATION: {destinationPacket}");
                Logger.Info($"SOURCE FINAL = {sourcePacket}");
                Logger.Info($"DESTINATION FINAL = {destinationPacket}");
                Logger.Info("===== NUEVO MVI HANDLER EJECUTADO =====");
                Logger.Info($"ASSEMBLY: {typeof(MviPacketHandler).Assembly.Location}");
                Logger.Info($"TYPE: {(byte)mviPacket.InventoryType}");
                Logger.Info($"SOURCE SLOT: {mviPacket.Slot}");
                Logger.Info($"PREVIOUS NULL: {previousInventory == null}");
                Logger.Info($"SOURCE PACKET: {sourcePacket}");
                Logger.Info($"DESTINATION PACKET: {destinationPacket}");

                // El oficial limpia primero el origen.
                Session.SendPacket(sourcePacket);

                // Luego actualiza el destino.
                Session.SendPacket(destinationPacket);

                if (mviPacket.InventoryType == InventoryType.Equipment)
                {
                    Session.SendPacket(
                        $"eff 1 {Session.Character.CharacterId} 4545");
                }
            }
        }

        #endregion
    }
}