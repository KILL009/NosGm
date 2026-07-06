using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;

namespace Frostvein.Handler.PacketHandler.Family
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