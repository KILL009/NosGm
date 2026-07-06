using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;
using System.Reactive.Linq;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class DropHandler : IPacketHandler
    {
        #region Instantiation

        public DropHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Drop(DropPacket dropPacket)
        {
            if (dropPacket != null)
            {
                //Session.AddLogsCmd(dropPacket);
                var vnum = dropPacket.VNum;
                var amount = dropPacket.Amount;
                if (amount < 1)
                    amount = 1;
                else if (amount > 999) amount = 999;
                var count = dropPacket.Count;
                if (count < 1) count = 1;
                var time = dropPacket.Time;

                if (amount < 0)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg($"Hmm, no there.", 0));
                    return;
                }

                if (Session.Character.LastDrop.AddSeconds(3) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 3 seconds before doing that again");
                    return;
                }

                var instance = Session.CurrentMapInstance;

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer =>
                {
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var droppedItem = new MonsterMapItem(Session.Character.PositionX,
                                Session.Character.PositionY, vnum, amount);
                            instance.DroppedList[droppedItem.TransportId] = droppedItem;
                            instance.Broadcast(
                                $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 -1");

                            Thread.Sleep(time * 1000 / count);
                        }
                    }
                });
            }
        }

        #endregion
    }
}