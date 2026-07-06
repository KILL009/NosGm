using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Raid.Threads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
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