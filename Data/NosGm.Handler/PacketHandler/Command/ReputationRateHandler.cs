using NosGm.Configuration;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ReputationRateHandler : IPacketHandler
    {
        #region Instantiation

        public ReputationRateHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ReputationRate(ReputationRatePacket reputationRatePacket)
        {
            if (reputationRatePacket != null)
            {
                //Session.AddLogsCmd(reputationRatePacket);
                if (reputationRatePacket.Value <= 1000)
                {
                    GameConfiguration.ReputationRate = reputationRatePacket.Value;

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REPUTATION_RATE_CHANGED"),
                            0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GoldRatePacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}