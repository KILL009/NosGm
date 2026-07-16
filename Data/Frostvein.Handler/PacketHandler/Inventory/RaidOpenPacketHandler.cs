using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.Packets.Packets.ClientPackets;
using System.Linq;
using System.Text;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class RaidOpenPacketHandler : IPacketHandler
    {
        private readonly ClientSession _session;

        public RaidOpenPacketHandler(ClientSession session)
        {
            _session = session;
        }

        public void OpenRaidInventory(RaidOpenPacket packet)
        {
            if (packet == null ||
                _session?.Character?.Inventory == null ||
                !_session.HasSelectedCharacter ||
                _session.IsDisposing)
            {
                return;
            }

            var raidItems = _session.Character.Inventory
                .GetAllItems()
                .Where(item =>
                    item != null &&
                    item.Type == InventoryType.Raid &&
                    item.Amount > 0 &&
                    item.Slot >= 0)
                .OrderBy(item => item.Slot)
                .ToList();

            var packetBuilder = new StringBuilder("inv 10");

            foreach (ItemInstance item in raidItems)
            {
                packetBuilder.Append(
                    $" {item.Slot}.{item.ItemVNum}.{item.Amount}");
            }

            string raidInventoryPacket = packetBuilder.ToString();

            Logger.Info($"RAID INVENTORY: {raidInventoryPacket}");
            _session.SendPacket(raidInventoryPacket);
        }
    }
}