using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Family
{
    public class FWithdrawPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FWithdrawPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        public void FamilyWithdraw(FWithdrawPacket fWithdrawPacket)
        {
            return;
        }
    }
}