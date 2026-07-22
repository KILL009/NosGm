using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class FactionsHandler : IPacketHandler
    {
        #region Instantiation

        public FactionsHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Faction(FactionPacket factionPacket)
        {
            if (ServerManager.Instance.ChannelId == 51)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"),
                        0));
                return;
            }

            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel
                || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"),
                        0));
                return;
            }

            if (factionPacket != null)
            {
                //Session.AddLogsCmd(factionPacket);
                Session.SendPacket("scr 0 0 0 0 0 0 0");
                if (Session.Character.Faction == FactionType.Angel)
                {
                    Session.Character.Faction = FactionType.Demon;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GET_PROTECTION_POWER_2"),
                            0));
                }
                else
                {
                    Session.Character.Faction = FactionType.Angel;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GET_PROTECTION_POWER_1"),
                            0));
                }

                Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                    Session.Character.CharacterId, 4799 + (byte)Session.Character.Faction));
                Session.SendPacket(Session.Character.GenerateFaction());
                if (ServerManager.Instance.ChannelId == 51) Session.SendPacket(Session.Character.GenerateFc());
            }
        }

        #endregion
    }
}