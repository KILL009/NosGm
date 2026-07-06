using Frostvein.Packets.Packets;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
{
    public class BrPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BrPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Br(BrPacket packet)
        {

            byte Rara = (byte)ServerManager.RandomNumber(0, 5); // Algo Random Kappa
            if (Rara == 4)
            {
                Random random = new Random();
                for (int i = 0; i < 5; i++)
                {
                    List<MapCell> possibilities = new List<MapCell>();
                    for (short x = -4; x < 5; x++)
                    {
                        for (short y = -4; y < 5; y++)
                        {
                            possibilities.Add(new MapCell { X = x, Y = y });
                        }
                    }

                    foreach (MapCell possibilitie in possibilities.OrderBy(s => random.Next()))
                    {
                        short mapx = (short)(packet.PosX + possibilitie.X);
                        short mapy = (short)(packet.PosY + possibilitie.Y);
                        if (!Session.CurrentMapInstance?.Map.IsBlockedZone(mapx, mapy) ?? false)
                        {
                            break;
                        }
                    }

                    if (Session.CurrentMapInstance != null)
                    {
                        short Rnd = (short)ServerManager.RandomNumber(0, 2);
                        short[] Mobs =
                        { 1042, // Magmaros
                          /*2513, // Bacoum*/
                          25 }; // Dander
                        MapMonster monster = new MapMonster
                        {
                            MonsterVNum = Mobs[ServerManager.RandomNumber(0, Mobs.Length)],
                            MapY = packet.PosY += Rnd,
                            MapX = packet.PosX += Rnd,
                            MapId = Session.Character.MapInstance.Map.MapId,
                            Position = Session.Character.Direction,
                            IsMoving = true,
                            MapMonsterId = Session.CurrentMapInstance.GetNextMonsterId(),
                            ShouldRespawn = false
                        };
                        monster.Initialize(Session.CurrentMapInstance);
                        Session.CurrentMapInstance.AddMonster(monster);
                        Session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                    }
                }
            }
            else
            {
                MapItem mapItem = Session.CurrentMapInstance.DroppedList[packet.ItemId];
                if (mapItem?.CanSpawn == true)
                {
                    MapInstance instance = Session.CurrentMapInstance;
                    if (Session.Character.Class == ClassType.Archer)
                    {
                        short[] StuffArcher =
                        {
                        4003, 4016, 4009,
                        4905, 4923, 4914,
                        4966, 4963, 4954,
                        4960, 4957, 4951,
                        4983, 4980, 4986
                    };
                        short[] SpArcher = { 903, 904, 911, 911, 912, 4501, 4495, 4492, 4488 };
                        int amount = 1;
                        MonsterMapItem droppedItem = new MonsterMapItem(packet.PosX, packet.PosY, StuffArcher[ServerManager.RandomNumber(0, StuffArcher.Length)], amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 0 -1");
                        MonsterMapItem DropSp = new MonsterMapItem(packet.PosX, packet.PosY, SpArcher[ServerManager.RandomNumber(0, SpArcher.Length)], amount);
                        instance.DroppedList[DropSp.TransportId] = DropSp;
                        instance.Broadcast($"drop {DropSp.ItemVNum} {DropSp.TransportId} {DropSp.PositionX - 2} {DropSp.PositionY} {(DropSp.GoldAmount > 1 ? DropSp.GoldAmount : DropSp.Amount)} 0 0 -1");
                    }
                    else if (Session.Character.Class == ClassType.Swordsman)
                    {
                        short[] StuffEscrimeur =
                        {
                        4001, 4007, 4013,
                        4902, 4920, 4911,
                        4964, 4961, 4952,
                        4958, 4955, 4949,
                        4981, 4978, 4984
                    };
                        short[] SpEscrimeur = { 901, 902, 909, 910, 4500, 4497, 4493, 4489 };
                        int amount = 1;
                        MonsterMapItem droppedItem = new MonsterMapItem(packet.PosX, packet.PosY, StuffEscrimeur[ServerManager.RandomNumber(0, StuffEscrimeur.Length)], amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 0 -1");
                        MonsterMapItem DropSp = new MonsterMapItem(packet.PosX, packet.PosY, SpEscrimeur[ServerManager.RandomNumber(0, SpEscrimeur.Length)], amount);
                        instance.DroppedList[DropSp.TransportId] = DropSp;
                        instance.Broadcast($"drop {DropSp.ItemVNum} {DropSp.TransportId} {DropSp.PositionX - 2} {DropSp.PositionY} {(DropSp.GoldAmount > 1 ? DropSp.GoldAmount : DropSp.Amount)} 0 0 -1");
                    }
                    else if (Session.Character.Class == ClassType.Magician)
                    {
                        short[] StuffMage =
                        {
                        4005, 4011, 4019,
                        4908, 4926, 4917,
                        4965, 4962, 4953,
                        4959, 4956, 4950,
                        4982, 4979, 4985
                    };
                        short[] SpMage = { 905, 906, 913, 914, 4502, 4499, 4491, 4487 };
                        int amount = 1;
                        MonsterMapItem droppedItem = new MonsterMapItem(packet.PosX, packet.PosY, StuffMage[ServerManager.RandomNumber(0, StuffMage.Length)], amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 0 -1");
                        MonsterMapItem DropSp = new MonsterMapItem(packet.PosX, packet.PosY, SpMage[ServerManager.RandomNumber(0, SpMage.Length)], amount);
                        instance.DroppedList[DropSp.TransportId] = DropSp;
                        instance.Broadcast($"drop {DropSp.ItemVNum} {DropSp.TransportId} {DropSp.PositionX - 2} {DropSp.PositionY} {(DropSp.GoldAmount > 1 ? DropSp.GoldAmount : DropSp.Amount)} 0 0 -1");
                    }
                    else if (Session.Character.Class == ClassType.MartialArtist)
                    {
                        short[] StuffMartial =
                        {
                        4734, 4733, 4732,
                        4731, 4730, 4735,
                        4736, 4765, 4766,
                        4770, 4472, 4752,
                        4750, 4484, 4754
                    };
                        short[] SpMartial = { 4486, 4485, 4437, 4532};
                        int amount = 1;
                        MonsterMapItem droppedItem = new MonsterMapItem(packet.PosX, packet.PosY, StuffMartial[ServerManager.RandomNumber(0, StuffMartial.Length)], amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 0 -1");
                        MonsterMapItem DropSp = new MonsterMapItem(packet.PosX, packet.PosY, SpMartial[ServerManager.RandomNumber(0, SpMartial.Length)], amount);
                        instance.DroppedList[DropSp.TransportId] = DropSp;
                        instance.Broadcast($"drop {DropSp.ItemVNum} {DropSp.TransportId} {DropSp.PositionX - 2} {DropSp.PositionY} {(DropSp.GoldAmount > 1 ? DropSp.GoldAmount : DropSp.Amount)} 0 0 -1");
                    }
                    else
                    {
                        return;
                    }
                    short[] Potion = { 1242, 9042, 5969, 9007, 9008, 9009, 278, 279, 280, 281, 4129, 4130, 4131, 4132 };
                    int Value = 1;
                    MonsterMapItem DroppedPotion = new MonsterMapItem(packet.PosX, packet.PosY, Potion[ServerManager.RandomNumber(0, Potion.Length)], Value);
                    instance.DroppedList[DroppedPotion.TransportId] = DroppedPotion;
                    instance.Broadcast($"drop {DroppedPotion.ItemVNum} {DroppedPotion.TransportId} {DroppedPotion.PositionX - 1} {DroppedPotion.PositionY} {(DroppedPotion.GoldAmount > 1 ? DroppedPotion.GoldAmount : DroppedPotion.Amount)} 0 0 -1");
                    short[] BuffPopo =
                    {
                    9021,
                     9022
                };
                    int ValueBuff = 1;
                    MonsterMapItem DroppedBuff = new MonsterMapItem(packet.PosX, packet.PosY, BuffPopo[ServerManager.RandomNumber(0, BuffPopo.Length)], ValueBuff);
                    instance.DroppedList[DroppedBuff.TransportId] = DroppedBuff;
                    instance.Broadcast($"drop {DroppedBuff.ItemVNum} {DroppedBuff.TransportId} {DroppedBuff.PositionX - 1} {DroppedBuff.PositionY + 1} {(DroppedBuff.GoldAmount > 1 ? DroppedBuff.GoldAmount : DroppedBuff.Amount)} 0 0 -1");
                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Object, packet.ItemId));
                    Session.CurrentMapInstance.DroppedList.Remove(packet.ItemId);
                }
            }

        }

        #endregion
    }
}