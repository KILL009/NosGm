using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;

namespace Frostvein.Handler.PacketHandler.Command
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