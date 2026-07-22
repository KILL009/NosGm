using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class UseItemPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseItemPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseItem(UseItemPacket useItemPacket)
        {
            if (useItemPacket == null || (byte)useItemPacket.Type >= 9)
            {
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                Session.SendPacket("info Action has been logged.");
                return;
            }

            ItemInstance itemInstance = Session.Character.Inventory.LoadBySlotAndType(useItemPacket.Slot, useItemPacket.Type);

            var packet = useItemPacket.OriginalContent.Split(' ', '^');

            if (packet.Length >= 2 && packet[1].Length > 0)
            {
                itemInstance?.Item.Use(Session, itemInstance, packet[1][0] == '#' ? (byte)255 : (byte)0, packet);
            }
        }

        #endregion
    }
}