using NosGm.Configuration;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.Handler.PacketHandler.Command
{
    public class Act4Handler : IPacketHandler
    {
        #region Instantiation

        public Act4Handler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Act4(Act4Packet act4Packet)
        {
            if (act4Packet != null)
            {
                if (ServerManager.Instance.IsAct4Online())
                {
                    switch (Session.Character.Faction)
                    {
                        case FactionType.None:
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, 51, 41);
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo("You need to be part of a faction to join Act 4"));
                            return;

                        case FactionType.Angel:
                            Session.Character.MapId = 130;
                            Session.Character.MapX = 12;
                            Session.Character.MapY = 40;
                            break;

                        case FactionType.Demon:
                            Session.Character.MapId = 131;
                            Session.Character.MapX = 12;
                            Session.Character.MapY = 40;
                            break;
                    }

                    Session.Character.Event.EmitEvent(new PlayerChangeChannelEvent(ServerConfiguration.IPAddress, Convert.ToInt32(ServerConfiguration.GlacernonServerPort), 3));
                }
                else
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, 51, 41);
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ACT4_OFFLINE")));
                }
            }

            Session.Character.GenerateSay(Act4Packet.ReturnHelp(), 10);
        }

        #endregion
    }
}