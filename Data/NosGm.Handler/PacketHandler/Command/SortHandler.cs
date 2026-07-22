using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using System;

namespace NosGm.Handler.PacketHandler.Command
{
    public class SortHandler : IPacketHandler
    {
        #region Instantiation

        public SortHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Sort(SortPacket sortPacket)
        {
            if (sortPacket?.InventoryType.HasValue == true)
            {
                var time = Session.Character.LastSort.AddSeconds(5);

                if (DateTime.Now <= time) // Anti spam
                {
                    Session.SendPacket(Session.Character.GenerateSay("Sort command is in cooldown, you have to wait 5 seconds to use it again", 11));
                    return;
                }

                ////Session.AddLogsCmd(sortPacket);
                if (sortPacket.InventoryType == InventoryType.Equipment || sortPacket.InventoryType == InventoryType.Etc || sortPacket.InventoryType == InventoryType.Main)

                {
                    Session.Character.Inventory.Reorder(Session, sortPacket.InventoryType.Value);
                }

            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SortPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}