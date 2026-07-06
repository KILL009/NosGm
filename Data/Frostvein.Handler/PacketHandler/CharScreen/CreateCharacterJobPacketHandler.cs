using Frostvein.Extension.GameExtension.Character;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using System.Threading.Tasks;

namespace Frostvein.Handler.BasicPacket.CharScreen
{
    internal class CreateCharacterJobPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CreateCharacterJobPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task CreateCharacterJob(CharacterJobCreatePacket characterCreatePacket)
        {
          
        }

        #endregion
    }
}