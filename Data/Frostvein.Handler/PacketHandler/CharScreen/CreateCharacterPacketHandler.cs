using Frostvein.Extension.GameExtension.Character;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.Handler.Packets.CharScreenPackets;
using System.Threading.Tasks;

namespace Frostvein.Handler.BasicPacket.CharScreen
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