using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class MobHandler : IPacketHandler
    {
        #region Instantiation

        public MobHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Mob(MobPacket mobPacket)
        {
            if (mobPacket != null)
            {
                //Session.AddLogsCmd(mobPacket);
                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    var npcmonster = ServerManager.GetNpcMonster(mobPacket.NpcMonsterVNum);
                    if (npcmonster == null) return;

                    var random = new Random();
                    for (var i = 0; i < mobPacket.Amount; i++)
                    {
                        var possibilities = new List<MapCell>();
                        for (short x = -4; x < 5; x++)
                            for (short y = -4; y < 5; y++)
                                possibilities.Add(new MapCell { X = x, Y = y });

                        foreach (var possibilitie in possibilities.OrderBy(s => random.Next()))
                        {
                            var mapx = (short)(Session.Character.PositionX + possibilitie.X);
                            var mapy = (short)(Session.Character.PositionY + possibilitie.Y);
                            if (!Session.CurrentMapInstance?.Map.IsBlockedZone(mapx, mapy) ?? false) break;
                        }

                        if (Session.CurrentMapInstance != null)
                        {
                            var monster = new MapMonster
                            {
                                MonsterVNum = mobPacket.NpcMonsterVNum,
                                MapY = Session.Character.PositionY,
                                MapX = Session.Character.PositionX,
                                MapId = Session.Character.MapInstance.Map.MapId,
                                Position = Session.Character.Direction,
                                IsMoving = mobPacket.IsMoving,
                                MapMonsterId = Session.CurrentMapInstance.GetNextMonsterId(),
                                ShouldRespawn = false
                            };
                            monster.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddMonster(monster);
                            Session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MobPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}