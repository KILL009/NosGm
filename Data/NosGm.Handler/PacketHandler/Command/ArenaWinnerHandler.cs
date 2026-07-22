using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ArenaWinnerHandler : IPacketHandler
    {
        #region Instantiation

        public ArenaWinnerHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ArenaWinner(ArenaWinnerPacket arenaWinner)
        {
            Session.Character.ArenaWinner = Session.Character.ArenaWinner == 0 ? 1 : 0;
            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}