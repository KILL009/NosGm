using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using System;
using System.Reactive.Linq;
using System.Threading;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ItemRainHandler : IPacketHandler
    {
        #region Instantiation

        public ItemRainHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ItemRain(ItemRainPacket itemRainPacket)
        {
            if (itemRainPacket != null)
            {
                //Session.AddLogsCmd(itemRainPacket);
                var vnum = itemRainPacket.VNum;
                var amount = itemRainPacket.Amount;
                if (amount > 999) amount = 999;
                var count = itemRainPacket.Count;
                var time = itemRainPacket.Time;

                var instance = Session.CurrentMapInstance;

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var cell = instance.Map.GetRandomPosition();
                        var droppedItem = new MonsterMapItem(cell.X, cell.Y, vnum, amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast(
                            $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 -1");

                        Thread.Sleep(time * 1000 / count);
                    }
                });
            }
        }

        #endregion
    }
}