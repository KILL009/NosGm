using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;

namespace NosGm.Handler.PacketHandler.Command
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