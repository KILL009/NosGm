using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class AddPetHandler : IPacketHandler
    {
        #region Instantiation

        public AddPetHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void AddPet(AddPetPacket addPetPacket)
        {
            if (addPetPacket != null)
            {
                Session.AddMate(addPetPacket.MonsterVNum, addPetPacket.Level, MateType.Pet);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddPartnerPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}