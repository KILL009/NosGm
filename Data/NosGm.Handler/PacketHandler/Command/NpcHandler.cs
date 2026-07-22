using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class NpcHandler : IPacketHandler
    {
        #region Instantiation

        public NpcHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Npc(NPCPacket NpcPacket)
        {
            if (NpcPacket != null)
            {
                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    var npcmonster = ServerManager.GetNpcMonster(NpcPacket.NpcMonsterVNum);
                    if (npcmonster == null) return;

                    var random = new Random();
                    for (var i = 0; i < NpcPacket.Amount; i++)
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
                            var npc = new MapNpc
                            {
                                NpcVNum = NpcPacket.NpcMonsterVNum,
                                MapY = Session.Character.PositionY,
                                MapX = Session.Character.PositionX,
                                MapId = Session.Character.MapInstance.Map.MapId,
                                Position = Session.Character.Direction,
                                IsMoving = NpcPacket.IsMoving,
                                ShouldRespawn = false,
                                MapNpcId = Session.CurrentMapInstance.GetNextNpcId()
                            };
                            npc.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddNPC(npc);
                            Session.CurrentMapInstance.Broadcast(npc.GenerateIn());
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(NPCPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}