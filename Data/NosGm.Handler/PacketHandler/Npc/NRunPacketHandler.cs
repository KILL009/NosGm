using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Npc
{
    public class NRunPacketHandler : IPacketHandler
    {
        #region Instantiation

        public NRunPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task NpcRunFunctionAsync(NRunPacket packet)
        {
            Session.Character.LastNRunId = packet.NpcId;
            Session.Character.LastItemVNum = 0;

            if (Session.Character.Hp > 0)
            {
                NRunHandler.NRun(Session, packet);
            }
        }

        #endregion
    }
}