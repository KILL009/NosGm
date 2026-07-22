using NosGm.Extension.Extension.Command;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.GameObject;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class CharStatHandler : IPacketHandler
    {
        #region Instantiation

        public CharStatHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void CharStat(CharacterStatsPacket characterStatsPacket)
        {
            var returnHelp = CharacterStatsPacket.ReturnHelp();
            if (characterStatsPacket != null)
            {
                var name = characterStatsPacket.CharacterName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (ServerManager.Instance.GetSessionByCharacterName(name) != null)
                    {
                        var character = ServerManager.Instance.GetSessionByCharacterName(name).Character;
                        Session.SendStats(character);
                    }
                    else if (DAOFactory.CharacterDAO.LoadByName(name) != null)
                    {
                        var characterDto = DAOFactory.CharacterDAO.LoadByName(name);
                        Session.SendStats(characterDto);
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
            }
        }

        #endregion
    }
}