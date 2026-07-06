using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
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
