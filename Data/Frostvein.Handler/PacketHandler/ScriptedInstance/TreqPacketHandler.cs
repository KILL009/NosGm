using Frostvein.Packets.Packets.ServerPackets;
using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
{
    public class TreqPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TreqPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GetTreq(TreqPacket treqPacket)
        {
            var packetSplit = treqPacket.OriginalContent.Split(' ');
            var timespace = Session.CurrentMapInstance.ScriptedInstances
                .Find(s => short.Parse(packetSplit[2]) == s.PositionX && short.Parse(packetSplit[3]) == s.PositionY).Copy();

            if (timespace != null)
            {
                if (packetSplit.Length == 5 && byte.Parse(packetSplit[4]) == 1)
                    Session.Character.EnterInstance(timespace);
                else
                    Session.SendPacket(timespace.GenerateRbr(Session.Character));
            }
        }

        #endregion
    }
}