using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;

namespace Frostvein.Handler.World.Bazaar
{
    public class RefreshPersonalListPacketHandler : IPacketHandler
    {
        private static readonly KeepAliveClient _keepAliveClient = KeepAliveClient.Instance;

        private ClientSession Session { get; set; }

        public RefreshPersonalListPacketHandler(ClientSession session) => Session = session;

        /// <summary>
        /// c_slist packet
        /// </summary>
        /// <param name="csListPacket"></param>
        public void RefreshPersonalBazarList(CSListPacket csListPacket)
        {
            if (!_keepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo($"Uh oh, it looks like the bazaar server is offline ! Please inform a staff member about it as soon as possible !"));
                return;
            }
            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return;
            }

            Session.SendPacket(CharacterExtension.GenerateRCSList(Session, csListPacket));
        }
    }
}

