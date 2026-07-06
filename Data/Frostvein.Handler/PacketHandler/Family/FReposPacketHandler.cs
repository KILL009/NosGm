using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Family
{
    public class FReposPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FReposPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        public void FamilyRepos(FReposPacket fReposPacket)
        {
            return;
        }
    }
}