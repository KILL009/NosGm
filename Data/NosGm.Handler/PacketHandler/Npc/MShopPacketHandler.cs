using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Npc
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