using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Inventory;

namespace NosGm.Handler.PacketHandler.Command
{
    public class UpgradeHandler : IPacketHandler
    {
        #region Instantiation

        public UpgradeHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Upgrade(UpgradeCommandPacket upgradePacket)
        {
            if (upgradePacket != null)
            {
                //Session.AddLogsCmd(upgradePacket);
                if (upgradePacket.Slot >= 0)
                {
                    var wearableInstance =
                        Session.Character.Inventory.LoadBySlotAndType(upgradePacket.Slot, 0);
                    wearableInstance?.UpgradeItem(Session, upgradePacket.Mode, upgradePacket.Protection, true);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}