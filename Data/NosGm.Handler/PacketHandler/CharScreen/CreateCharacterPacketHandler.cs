using NosGm.Extension.GameExtension.Character;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.Handler.Packets.CharScreenPackets;
using System.Threading.Tasks;

namespace NosGm.Handler.BasicPacket.CharScreen
{
    internal class CreateCharacterPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CreateCharacterPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods



        public void CreateCharacter(CharacterCreatePacket characterCreatePacket)
        {
            if (characterCreatePacket.Name == null)
            {
                Session.SendPacket($"say 1 0 10 That won't work.");
                return;
            }
            Session.CreateCharacterAction(characterCreatePacket, ClassType.Adventurer);
        }

        #endregion
    }
}