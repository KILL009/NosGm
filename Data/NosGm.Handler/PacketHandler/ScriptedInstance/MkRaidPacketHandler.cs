using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Raid.Threads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.ScriptedInstance
{
    public class MkRaidPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MkRaidPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task GenerateRaid(MkRaidPacket mkRaidPacket)
        {
            await RaidStartThread.Run(Session);
        }

        #endregion
    }
}