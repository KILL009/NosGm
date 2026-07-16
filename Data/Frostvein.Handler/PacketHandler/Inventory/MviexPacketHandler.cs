using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.Packets.Packets.ClientPackets;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class MviexPacketHandler : IPacketHandler
    {
        private const byte RaidInventoryId = 10;
        private const short RaidInventorySize = 28;

        public MviexPacketHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void MoveRaidItem(MviexPacket packet)
        {
            if (packet == null ||
                Session?.Character?.Inventory == null ||
                !Session.HasSelectedCharacter ||
                Session.IsDisposing)
            {
                return;
            }

            if (packet.InventoryType != RaidInventoryId ||
                packet.Amount <= 0 ||
                packet.Slot < 0 ||
                packet.DestinationSlot < 0 ||
                packet.Slot >= RaidInventorySize ||
                packet.DestinationSlot >= RaidInventorySize ||
                packet.Slot == packet.DestinationSlot ||
                Session.Character.InExchangeOrTrade)
            {
                return;
            }

            lock (Session.Character.Inventory)
            {
                ItemInstance sourceItem =
                    Session.Character.Inventory.LoadBySlotAndType(
                        packet.Slot,
                        InventoryType.Raid);

                if (sourceItem == null ||
                    sourceItem.Amount < packet.Amount)
                {
                    Session.Character.Inventory.SendRaidInventory();
                    return;
                }

                Session.Character.Inventory.MoveItem(
                    InventoryType.Raid,
                    InventoryType.Raid,
                    packet.Slot,
                    packet.Amount,
                    packet.DestinationSlot,
                    out _,
                    out _);

                // Raid siempre se sincroniza completo.
                Session.Character.Inventory.SendRaidInventory();
            }
        }
    }
}