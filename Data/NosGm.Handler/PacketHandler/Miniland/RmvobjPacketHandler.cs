using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Miniland
{
    public class RmvobjPacketHandler : IPacketHandler
    {
        #region Instantiation

        public RmvobjPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MinilandRemoveObject(RmvobjPacket packet)
        {
            var minilandobject =
                Session.Character.Inventory.LoadBySlotAndType(packet.Slot, InventoryType.Miniland);
            if (minilandobject != null)
            {
                if (Session.Character.MinilandState == MinilandState.Lock)
                {
                    var minilandObject =
                        Session.Character.MinilandObjects.Find(s => s.ItemInstanceId == minilandobject.Id);
                    if (minilandObject != null)
                    {
                        Session.Character.MinilandObjects.Remove(minilandObject);
                        Session.SendPacket(minilandObject.GenerateMinilandEffect(true));
                        Session.SendPacket(Session.Character.GenerateMinilandPoint());
                        Session.SendPacket(minilandObject.GenerateMinilandObject(true));
                    }
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_NEED_LOCK"), 0));
                }
            }
        }

        #endregion
    }
}