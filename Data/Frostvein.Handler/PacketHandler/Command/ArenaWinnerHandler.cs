using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
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