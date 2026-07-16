using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.Packets.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.Packets.basic
{
    public class ISortPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession Session;

        private static readonly HashSet<byte> AllowedClientInventoryIds =
            new HashSet<byte>
            {
                0,  // Equipment
                1,  // Main
                2,  // Etc
                3,  // Miniland
                6,  // Specialist
                7,  // Costume
                8,  // Wear
                9   // Bazaar
            };

        #endregion

        #region Instantiation

        public ISortPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Methods

        public void Sort(isortPacket packet)
        {
            if (packet == null ||
                Session?.Character?.Inventory == null ||
                !Session.HasSelectedCharacter ||
                Session.IsDisposing)
            {
                return;
            }

            Logger.Info($"ISORT CLIENT TYPE: {packet.Type}");

            /*
             * El cliente usa 10 para el inventario Raid.
             *
             * Internamente Frostvein usa:
             * Warehouse = 10
             * Raid = 23
             *
             * No debemos convertir isort 10 a Warehouse.
             *
             * El servidor oficial responde con msgi 3 1808...
             * cuando se pulsa Sort en Raid.
             */
            if (packet.Type == 10)
            {
                Logger.Info("ISORT: Raid sort blocked like official server.");
                SendErrorMessage();
                return;
            }

            if (!AllowedClientInventoryIds.Contains(packet.Type))
            {
                Logger.Info(
                    $"ISORT: unsupported client inventory ID {packet.Type}");

                SendErrorMessage();
                return;
            }

            DateTime nextAllowedSort =
                Session.Character.LastISort.AddSeconds(5);

            if (DateTime.Now <= nextAllowedSort)
            {
                Logger.Info("ISORT: cooldown active.");
                SendErrorMessage();
                return;
            }

            InventoryType? inventoryType =
                ConvertClientInventoryId(packet.Type);

            if (!inventoryType.HasValue)
            {
                Logger.Info(
                    $"ISORT: could not convert client ID {packet.Type}");

                SendErrorMessage();
                return;
            }

            Session.Character.LastISort = DateTime.Now;

            Session.Character.Inventory.Reorder(
                Session,
                inventoryType.Value);
        }

        private static InventoryType? ConvertClientInventoryId(
            byte clientInventoryId)
        {
            switch (clientInventoryId)
            {
                case 0:
                    return InventoryType.Equipment;

                case 1:
                    return InventoryType.Main;

                case 2:
                    return InventoryType.Etc;

                case 3:
                    return InventoryType.Miniland;

                case 6:
                    return InventoryType.Specialist;

                case 7:
                    return InventoryType.Costume;

                case 8:
                    return InventoryType.Wear;

                case 9:
                    return InventoryType.Bazaar;

                default:
                    return null;
            }
        }

        private void SendErrorMessage()
        {
            Session.SendPacket("msgi 3 1808 0 0 0 0 0");
        }

        #endregion
    }
}