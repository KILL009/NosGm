using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ChangeFairyLevelHandler : IPacketHandler
    {
        #region Instantiation

        public ChangeFairyLevelHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ChangeFairyLevel(ChangeFairyLevelPacket changeFairyLevelPacket)
        {
            var fairy = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (changeFairyLevelPacket != null)
            {
                if (fairy != null)
                {
                    fairy.ElementRate = changeFairyLevelPacket.FairyLevel;
                    fairy.XP = 0;
                    Session.SendPacket(Session.Character.GeneratePairy());
                    MessageExtension.SendGreen(Session, $"Fairy Level was set to {changeFairyLevelPacket.FairyLevel}");
                    MessageExtension.SendBubble(Session, $"Fairy Level was set to {changeFairyLevelPacket.FairyLevel}");
                }
            }
        }

        #endregion
    }
}