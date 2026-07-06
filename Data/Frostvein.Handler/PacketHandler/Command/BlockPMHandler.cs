using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class BlockPMHandler : IPacketHandler
    {
        #region Instantiation

        public BlockPMHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void BlockPm(BlockPMPacket blockPmPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(!Session.Character.GmPvtBlock ? "GM_BLOCK_ENABLE" : "GM_BLOCK_DISABLE"),
                    10));
            Session.Character.GmPvtBlock = !Session.Character.GmPvtBlock;
        }

        #endregion
    }
}