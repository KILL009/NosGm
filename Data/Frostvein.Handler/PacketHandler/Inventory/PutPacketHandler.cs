using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;
using System.Reactive.Linq;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class PutPacketHandler : IPacketHandler
    {
        #region Instantiation

        public PutPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion


        public void PutItem(PutPacket putPacket)
        {
           
            if (putPacket == null || Session.Character.HasShopOpened)
            {
                return;
            }

           
            lock (Session.Character.Inventory)
            {
                ItemInstance invitem =
                    Session.Character.Inventory.LoadBySlotAndType(putPacket.Slot, putPacket.InventoryType);
                if (invitem?.Item.IsDroppable == true && invitem.Item.IsTradable
                    && !Session.Character.InExchangeOrTrade && putPacket.InventoryType != InventoryType.Bazaar)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount < 1000)
                    {
                        if (Session.Character.MapInstance.DroppedList.Count < 200 && Session.HasCurrentMapInstance)
                        {
                            MapItem droppedItem = Session.CurrentMapInstance.PutItem(putPacket.InventoryType,
                                putPacket.Slot, putPacket.Amount, ref invitem, Session);
                            if (droppedItem == null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("ITEM_NOT_DROPPABLE_HERE"), 0));
                                return;
                            }

                            Session.SendPacket(invitem.GenerateInventoryAdd());

                            if (invitem.Amount == 0)
                            {
                                Session.Character.DeleteItem(invitem.Type, invitem.Slot);
                            }

                            Logger.LogUserEvent("CHARACTER_ITEM_DROP", Session.GenerateIdentity(),
                                $"[PutItem]IIId: {invitem.Id} ItemVNum: {droppedItem.ItemVNum} Amount: {droppedItem.Amount} MapId: {Session.CurrentMapInstance.Map.MapId} MapX: {droppedItem.PositionX} MapY: {droppedItem.PositionY}");
                            Session.CurrentMapInstance?.Broadcast(
                                $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {droppedItem.Amount} 0 -1");
                        }
                        else
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("DROP_MAP_FULL"),
                                    0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_DROP_AMOUNT"), 0));
                    }
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_DROPPABLE"), 0));
                }
            }
        }
    }
}