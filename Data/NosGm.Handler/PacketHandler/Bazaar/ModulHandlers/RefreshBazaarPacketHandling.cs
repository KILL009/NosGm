using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.HttpClients;

namespace NosGm.Handler.Bazaar
{
    public class RefreshBazaarPacketHandling : IPacketHandler
    {
        #region Instantiation

        public RefreshBazaarPacketHandling(ClientSession session) => Session = session;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        private static readonly KeepAliveClient _keepAliveClient = KeepAliveClient.Instance;

        #endregion Properties

        #region Methods

        public void RefreshBazarList(CBListPacket cbListPacket)
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

            Session.SendPacket(UserInterfaceHelper.GenerateRCBList(cbListPacket));
        }

        #endregion Methods
    }
}