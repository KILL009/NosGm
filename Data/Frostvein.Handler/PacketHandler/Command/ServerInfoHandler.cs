using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.Master.Library.Client;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ServerInfoHandler : IPacketHandler
    {
        #region Instantiation

        public ServerInfoHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ServerInfo(ServerInfoPacket serverInfoPacket)
        {
            //Session.AddLogsCmd(serverInfoPacket);
            Session.SendPacket(Session.Character.GenerateSay("------------Server Info------------", 11));

            long ActualChannelId = 0;

            CommunicationServiceClient.Instance.GetOnlineCharacters()
                .Where(s => serverInfoPacket.ChannelId == null || s[1] == serverInfoPacket.ChannelId).OrderBy(s => s[1])
                .ToList().ForEach(s =>
                {
                    if (s[1] > ActualChannelId)
                    {
                        if (ActualChannelId > 0)
                            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------",
                                11));
                        ActualChannelId = s[1];
                        Session.SendPacket(
                            Session.Character.GenerateSay($"-------------Channel:{ActualChannelId}-------------", 11));
                    }

                    var Character = DAOFactory.CharacterDAO.LoadById(s[0]);
                    Session.SendPacket(
                        Session.Character.GenerateSay(
                            $"CharacterName: {Character.Name} | CharacterId: {Character.CharacterId} | SessionId: {s[2]}",
                            12));
                });

            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
        }

        #endregion
    }
}