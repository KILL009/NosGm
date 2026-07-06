using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ChannelInfoHandler : IPacketHandler
    {
        #region Instantiation

        public ChannelInfoHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ChannelInfo(ChannelInfoPacket channelInfoPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay(
                $"-----------Channel Info-----------\n-------------Channel:{ServerManager.Instance.ChannelId}-------------",
                11));
            foreach (var session in ServerManager.Instance.Sessions)
                Session.SendPacket(
                    Session.Character.GenerateSay(
                        $"CharacterName: {session.Character.Name} | CharacterId: {session.Character.CharacterId} | SessionId: {session.SessionId}",
                        12));

            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
        }

        #endregion
    }
}