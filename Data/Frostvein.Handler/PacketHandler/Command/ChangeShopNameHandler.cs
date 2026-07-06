using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ChangeShopNameHandler : IPacketHandler
    {
        #region Instantiation

        public ChangeShopNameHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ChangeShopName(ChangeShopNamePacket changeShopNamePacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                if (!string.IsNullOrEmpty(changeShopNamePacket.Name))
                {
                    if (Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId) is MapNpc npc)
                    {
                        if (npc.Shop is Shop shop)
                        {
                            if (DAOFactory.ShopDAO.LoadById(shop.ShopId) is ShopDTO shopDTO)
                            {
                                shop.Name = changeShopNamePacket.Name;
                                shopDTO.Name = changeShopNamePacket.Name;
                                DAOFactory.ShopDAO.Update(ref shopDTO);

                                Session.CurrentMapInstance.Broadcast(
                                    $"shop 2 {npc.MapNpcId} {npc.Shop.ShopId} {npc.Shop.MenuType} {npc.Shop.ShopType} {npc.Shop.Name}");
                            }
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"),
                                11));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(ChangeShopNamePacket.ReturnHelp(), 10));
                }
            }
        }

        #endregion
    }
}