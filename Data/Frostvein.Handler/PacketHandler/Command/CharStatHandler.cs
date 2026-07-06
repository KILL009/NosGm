using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Command
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