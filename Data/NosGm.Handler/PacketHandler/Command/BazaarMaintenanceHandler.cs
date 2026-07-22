using NosTale.Packets.Packets.CommandPackets;
using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenNos.Handler.PacketHandler.Command
{
    public class BazaarMaintenanceHandler : IPacketHandler
    {
        #region Instantiation

        public BazaarMaintenanceHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Command(BazaarMaintenance helpPacket)
        {
            ServerManager.Instance.BaazarMaintenance();
        }

        #endregion
    }
}