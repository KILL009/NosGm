using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class BlDelPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BlDelPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; } 

        #endregion

        #region Methods

        public void BlacklistDelete(BlDelPacket blDelPacket)
        {
            Session.Character.DeleteBlackList(blDelPacket.CharacterId);
            Session.SendPacket(Session.Character.GenerateBlinit());
            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_DELETED")));
        }

        #endregion
    }
}