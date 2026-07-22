using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Command
{
    public class EditorModeHandler : IPacketHandler
    {
        #region Instantiation

        public EditorModeHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void EditorMode(EditorModePacket editorMode)
        {
            if (Session.Character.EditorMode)
            {
                Session.Character.EditorMode = false;
                MessageExtension.SendRed(Session, "Editor Mode has been turned off");
            }
            else
            {
                Session.Character.EditorMode = true;
                MessageExtension.SendGreen(Session, "Editor Mode has been turned on");
            }
        }

        #endregion
    }
}