using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ChangeSexHandler : IPacketHandler
    {
        #region Instantiation

        public ChangeSexHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ChangeGender(ChangeSexPacket changeSexPacket)
        {
            Session.Character.Event.EmitEvent(new ChangeSexEvent());
        }

        #endregion
    }
}