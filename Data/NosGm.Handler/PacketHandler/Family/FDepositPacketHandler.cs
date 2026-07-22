using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;

namespace NosGm.Handler.PacketHandler.Family
{
    public class FDepositPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FDepositPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        public void FamilyDeposit(FDepositPacket fDepositPacket)
        {
            return;
        }
    }
}