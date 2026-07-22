using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.Packets.Packets.ClientPackets;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class Mve2PacketHandler : IPacketHandler
    {
        #region Constants

        private const short RaidInventorySize = 28;

        #endregion

        #region Members

        private static readonly HashSet<short> RaidItemVNums =
            new HashSet<short>
            {
                302,   // Caja del raid
                1094,  // Fragmento de time-space (raid)

                1127,  // Sello del raid de la Mamá Cubi
                1128,  // Sello del raid de Ginseng
                1129,  // Sello del raid de Dark Castra
                1130,  // Sello del raid de la Araña Negra Gigante
                1131,  // Sello del raid del Slade gigante

                1150,  // Sello del raid de la sombra de Zenas
                1195,  // Sello del raid del Rey Pollo
                1226,  // Sello del raid de Namaju
                1234,  // Sello del raid de Graslin
                1371,  // Sello del raid del muñeco de nieve

                1892,  // Sello del raid de Ibrahim
                1916,  // Sello del raid de Jack Calabazas

                4083,  // Caja del raid (evento)
                5109,  // Sello del raid de la Reina Gallina
                5140,  // Sello del raid de Pete O'Peng

                5500,  // Sello del raid de Lord Draco
                5512,  // Sello del raid de Glacerus

                5730,  // Sello del raid del Conejo de Pascua
                5734   // Caja del raid del Conejo de Pascua
            };

        #endregion

        #region Instantiation

        public Mve2PacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MoveExtendedInventoryItem(Mve2Packet packet)
        {
            if (packet == null ||
                Session?.Character?.Inventory == null ||
                !Session.HasSelectedCharacter ||
                Session.IsDisposing)
            {
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            InventoryType? sourceTypeResult =
                ConvertClientInventoryId(packet.SourceInventoryId);

            InventoryType? destinationTypeResult =
                ConvertClientInventoryId(packet.DestinationInventoryId);

            if (!sourceTypeResult.HasValue ||
                !destinationTypeResult.HasValue)
            {
                Logger.Info(
                    $"MVE2 blocked: unknown inventory IDs " +
                    $"{packet.SourceInventoryId} -> " +
                    $"{packet.DestinationInventoryId}");

                return;
            }

            InventoryType sourceType = sourceTypeResult.Value;
            InventoryType destinationType = destinationTypeResult.Value;

            bool mainToRaid =
                sourceType == InventoryType.Main &&
                destinationType == InventoryType.Raid;

            bool raidToMain =
                sourceType == InventoryType.Raid &&
                destinationType == InventoryType.Main;

            if (!mainToRaid && !raidToMain)
            {
                Logger.Info(
                    $"MVE2 blocked: invalid combination " +
                    $"{sourceType} -> {destinationType}");

                return;
            }

            if (packet.SourceSlot < 0 ||
                packet.DestinationSlot < 0)
            {
                return;
            }

            int mainInventorySize =
                Session.Character.Inventory.BackpackSize();

            if (sourceType == InventoryType.Main &&
                packet.SourceSlot >= mainInventorySize)
            {
                return;
            }

            if (destinationType == InventoryType.Main &&
                packet.DestinationSlot >= mainInventorySize)
            {
                return;
            }

            if (sourceType == InventoryType.Raid &&
                packet.SourceSlot >= RaidInventorySize)
            {
                return;
            }

            if (destinationType == InventoryType.Raid &&
                packet.DestinationSlot >= RaidInventorySize)
            {
                return;
            }

            lock (Session.Character.Inventory)
            {
                ItemInstance sourceItem =
                    Session.Character.Inventory.LoadBySlotAndType(
                        packet.SourceSlot,
                        sourceType);

                if (sourceItem == null ||
                    sourceItem.Amount <= 0)
                {
                    Logger.Info(
                        $"MVE2: source missing at " +
                        $"{sourceType}:{packet.SourceSlot}");

                    ResyncInventories();
                    return;
                }

                if (destinationType == InventoryType.Raid &&
                    !RaidItemVNums.Contains(sourceItem.ItemVNum))
                {
                    Logger.Info(
                        $"MVE2: VNum {sourceItem.ItemVNum} " +
                        $"is not allowed in Raid inventory.");

                    Session.SendPacket(
                        "msg 4 Este objeto no puede guardarse " +
                        "en el inventario de raid.");

                    ResyncInventories();
                    return;
                }

                ItemInstance destinationItem =
                    Session.Character.Inventory.LoadBySlotAndType(
                        packet.DestinationSlot,
                        destinationType);

                if (destinationItem != null)
                {
                    Logger.Info(
                        $"MVE2: destination occupied by VNum " +
                        $"{destinationItem.ItemVNum}");

                    // Por ahora se bloquean swap y apilamiento.
                    // Se reconstruyen ambos inventarios para evitar fantasmas.
                    ResyncInventories();
                    return;
                }

                short movedAmount = sourceItem.Amount;

                Session.Character.Inventory.MoveItem(
                    sourceType,
                    destinationType,
                    packet.SourceSlot,
                    movedAmount,
                    packet.DestinationSlot,
                    out _,
                    out _);

                ItemInstance sourceAfterMove =
                    Session.Character.Inventory.LoadBySlotAndType(
                        packet.SourceSlot,
                        sourceType);

                ItemInstance destinationAfterMove =
                    Session.Character.Inventory.LoadBySlotAndType(
                        packet.DestinationSlot,
                        destinationType);

                if (destinationAfterMove == null)
                {
                    Logger.Info(
                        $"MVE2 failed: destination is empty after MoveItem. " +
                        $"{sourceType}:{packet.SourceSlot} -> " +
                        $"{destinationType}:{packet.DestinationSlot}");

                    ResyncInventories();
                    return;
                }

                DAOFactory.ItemInstanceDAO.InsertOrUpdate(
                    destinationAfterMove);

                if (sourceAfterMove != null)
                {
                    DAOFactory.ItemInstanceDAO.InsertOrUpdate(
                        sourceAfterMove);
                }

                string sourcePacket =
                    sourceAfterMove != null
                        ? GenerateSlot(sourceAfterMove)
                        : GenerateEmptySlot(
                            sourceType,
                            packet.SourceSlot);

                string destinationPacket =
                    GenerateSlot(destinationAfterMove);

                if (string.IsNullOrWhiteSpace(sourcePacket) ||
                    string.IsNullOrWhiteSpace(destinationPacket))
                {
                    Logger.Info(
                        $"MVE2 failed generating packets. " +
                        $"SourceType={sourceType}, " +
                        $"DestinationType={destinationAfterMove.Type}");

                    ResyncInventories();
                    return;
                }

                Logger.Info($"MVE2 SOURCE: {sourcePacket}");
                Logger.Info($"MVE2 DESTINATION: {destinationPacket}");

                // Primero se envía el cambio individual como hace el oficial.
                Session.SendPacket(sourcePacket);
                Session.SendPacket(destinationPacket);

                // Después se reconstruyen una sola vez para eliminar fantasmas.
                ResyncInventories();
            }
        }

        private static InventoryType? ConvertClientInventoryId(
            byte clientInventoryId)
        {
            switch (clientInventoryId)
            {
                case 1:
                    return InventoryType.Main;

                case 10:
                    return InventoryType.Raid;

                default:
                    return null;
            }
        }

        private static string GenerateEmptySlot(
            InventoryType type,
            short slot)
        {
            switch (type)
            {
                case InventoryType.Main:
                    return $"inv 1 {slot}.0.0.0";

                case InventoryType.Raid:
                    return $"inv 10 {slot}.0.0.0";

                default:
                    return string.Empty;
            }
        }

        private static string GenerateSlot(ItemInstance item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            switch (item.Type)
            {
                case InventoryType.Main:
                    return
                        $"inv 1 {item.Slot}." +
                        $"{item.ItemVNum}." +
                        $"{item.Amount}.0";

                case InventoryType.Raid:
                    return
                        $"inv 10 {item.Slot}." +
                        $"{item.ItemVNum}." +
                        $"{item.Amount}.0";

                default:
                    return string.Empty;
            }
        }

        private void ResyncInventories()
        {
            if (Session?.Character?.Inventory == null)
            {
                return;
            }

            RefreshMainInventory();

            // Envía todo Raid en un único paquete combinado.
            Session.Character.Inventory.SendRaidInventory();
        }

        private void RefreshMainInventory()
        {
            if (Session?.Character?.Inventory == null)
            {
                return;
            }

            int mainInventorySize =
                Session.Character.Inventory.BackpackSize();

            var mainItems = Session.Character.Inventory
                .GetAllItems()
                .Where(item =>
                    item != null &&
                    item.Type == InventoryType.Main &&
                    item.Amount > 0 &&
                    item.Slot >= 0 &&
                    item.Slot < mainInventorySize)
                .OrderBy(item => item.Slot)
                .ToList();

            // Limpia visualmente Main.
            Session.SendPacket("inv 1");

            // Main se reconstruye usando ivn 1.
            foreach (ItemInstance item in mainItems)
            {
                Session.SendPacket(
                    $"ivn 1 {item.Slot}." +
                    $"{item.ItemVNum}." +
                    $"{item.Amount}.0");
            }
        }

        #endregion
    }
}