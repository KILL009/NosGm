using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class PartnerSpXpHandler : IPacketHandler
    {
        #region Instantiation

        public PartnerSpXpHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void PartnerSpXp(PartnerSpXpPacket partnerSpXpPacket)
        {
            if (partnerSpXpPacket == null) return;
            //Session.AddLogsCmd(partnerSpXpPacket);
            var mate = Session.Character.Mates?.ToList()
                .FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner);

            if (mate?.Sp != null)
            {
                mate.Sp.FullXp();
                Session.SendPacket(mate.GenerateScPacket());
            }
        }

        #endregion
    }
}