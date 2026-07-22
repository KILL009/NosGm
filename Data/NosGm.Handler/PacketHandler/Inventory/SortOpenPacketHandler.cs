using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class SortOpenPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SortOpenPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SortOpen(SortOpenPacket sortOpenPacket)
        {
            if (sortOpenPacket != null)
            {
                var gravity = true;
                while (gravity)
                {
                    gravity = false;
                    for (short i = 0; i < 2; i++)
                    {
                        for (short x = 0; x < 44; x++)
                        {
                            var type = i == 0 ? InventoryType.Specialist : InventoryType.Costume;
                            if (Session.Character.Inventory.LoadBySlotAndType(x, type) == null
                                && Session.Character.Inventory.LoadBySlotAndType((short)(x + 1), type)
                                != null)
                            {
                                Session.Character.Inventory.MoveItem(type, type, (short)(x + 1), 1, x,
                                    out var _, out var invdest);
                                Session.SendPacket(invdest.GenerateInventoryAdd());
                                Session.Character.DeleteItem(type, (short)(x + 1));
                                gravity = true;
                            }
                        }

                        Session.Character.Inventory.Reorder(Session,
                            i == 0 ? InventoryType.Specialist : InventoryType.Costume);
                    }
                }
            }
        }

        #endregion
    }
}