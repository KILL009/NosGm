using NosGm.Extension.GameExtension.Character;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using System.Threading.Tasks;

namespace NosGm.Handler.BasicPacket.CharScreen
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