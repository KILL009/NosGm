using NosGm.Configuration;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Packets.ClientPackets;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class BpMselPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BpMselPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task BattlePassGetPoint(BpMselPacket packet)
        {
            if (Session == null)
            {
                return;
            }
            if (!GameConfiguration.BattlePassEnabled || ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            if (Session.Character == null)
            {
                return;
            }

            var bpQuest = ServerManager.Instance.BattlePassQuests.FirstOrDefault(b => b.BpQuestId == packet.QuestId);

            bool isPremium = Session.Character.HasPremiumBattlePass;

            if (bpQuest == null)
            {
                return;
            }

            if (bpQuest.IsPremium && !isPremium)
            {
                return;
            }

            var bpQuestProgress = Session.Character.BattlePassQuestProgresses.FirstOrDefault(b => b.BpQuestId == packet.QuestId);

            if (bpQuestProgress == null)
            {
                return;
            }

            if (bpQuest.Amount == bpQuestProgress.Amount && !bpQuestProgress.Completed)
            {
                Session.Character.BattlePassPoints += bpQuest.Points;
                bpQuestProgress.Completed = true;
                Session.SendPacket(Session.Character.GenerateBpQuest());
                Session.SendPacket(Session.Character.GenerateBp2Quest());
                Session.SendPacket(Session.Character.GenerateBppPacket());
            }
        }

        #endregion
    }
}