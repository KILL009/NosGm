using NosGm.Configuration;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class BpOpenPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BpOpenPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task BpOpenAsync(BpOpenPacket bpOpenPacket)
        {
            if (!GameConfiguration.BattlePassEnabled || ServerManager.Instance.ChannelId == 51)
            {
                return;
            }
            Session.SendPacket("bpo");
            Session.SendPacket("bpm 70 2 1800 22031000 22042100 0 14 0 0 2 0 5 0 1 17 0 0 0 5 5 0 0 34");
            Session.SendPacket(Session.Character.GenerateBptPacket());
            Session.SendPacket(Session.Character.GenerateBppPacket());
        }

        public async Task BpOpenAsyncNotCustom(BpOpenPacket bpOpenPacket)
        {
            if (!GameConfiguration.BattlePassEnabled || ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            Session.SendPacket(Session.Character.GenerateBpQuest());
            Session.SendPacket(Session.Character.GenerateBp2Quest());
            Session.SendPacket(Session.Character.GenerateBptPacket());
            Session.SendPacket(Session.Character.GenerateBppPacket());
            Session.SendPacket("bpo");
        }

        public async Task BpOpenAsyncCustom(BpOpenPacket bpOpenPacket)
        {
            if (!GameConfiguration.BattlePassEnabled || ServerManager.Instance.ChannelId == 51)
            {
                return;
            }
            Session.SendPacket("bpo");
            Session.SendPacket("bpm 70 2 1800 22031000 22042100 0 14 0 0 2 0 5 1 1 17 0 0 0 5 5 1 2 34 0 0 2 5911 5 1");
            Session.SendPacket(Session.Character.GenerateBptPacket());
            Session.SendPacket(Session.Character.GenerateBppPacket());
        }

        #endregion
    }
}