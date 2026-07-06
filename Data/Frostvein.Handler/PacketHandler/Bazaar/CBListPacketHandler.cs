using NosTale.Configuration;
using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CBListPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CBListPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task RefreshBazarListAsync(CBListPacket cbListPacket)
        {
            if (!GameConfiguration.BazaarEnabled)
            {
                Session.SendPacket("info The Bazaar Server is currently offline");
                return;
            }
            if (Session.Character.Level < 85)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }
            if (Session.Character.Reputation < 90000)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }

            if (Session.Character.Channel.ChannelId > 1)
            {
                Session.SendPacket("info The NosBazaar can only be accessed on Channel 1");
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }

            if (Session.Character.IsMuted())
            {
                await Session.SendPacketAsync("info This does not work when you are being muted");
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                return;
            }

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            Session.SendPacket(UserInterfaceHelper.GenerateRCBList(cbListPacket));
            Session.SendPacket("rc_reg 1");

        }

        #endregion
    }
}