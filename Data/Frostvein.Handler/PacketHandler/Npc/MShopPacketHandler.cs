using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Npc
{
    public class MShopPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MShopPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task CreateShopAsync(MShopPacket packet)
        {
            Session.SendPacket(Session.Character.DisplayAllPrimalQuest());
        }
        #endregion
    }
}