using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System;
using System.Linq;

namespace NosGm.Handler.PacketHandler.ScriptedInstance
{
    public class SReqPacketHandler : IPacketHandler
    {     
        #region Instantiation

        public SReqPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void LaunchTower(SreqPacket packet)
        {
           
        }

        #endregion
    }
}
