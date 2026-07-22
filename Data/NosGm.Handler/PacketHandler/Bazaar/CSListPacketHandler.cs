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
    public class CSListPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CSListPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task RefreshPersonalBazarListAsync(CSListPacket csListPacket)
        {
            if (ServerManager.Instance.InShutdown)
            {
                return;
            }
            if (Session.Character.IsMuted())
            {
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

            if (Session.Character.Channel.ChannelId > 1)
            {
                Session.SendPacket("info The NosBazaar can only be accessed on Channel 1");
                return;
            }

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

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            await Session.SendPacketAsync(Session.Character.GenerateRCSList(csListPacket));
            Session.SendPacket("rc_reg 1");
        }

        #endregion
    }
}