using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class BackMobHandler : IPacketHandler
    {
        #region Instantiation

        public BackMobHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void BackMob(BackMobPacket backMobPacket)
        {
            if (backMobPacket != null)
            {
                if (!Session.HasCurrentMapInstance) return;
                var lastObject = Session.CurrentMapInstance.RemovedMobNpcList.LastOrDefault();

                if (lastObject is MapMonster mapMonster)
                {
                    var backMonst = new MapMonsterDTO
                    {
                        MonsterVNum = mapMonster.MonsterVNum,
                        MapX = mapMonster.MapX,
                        MapY = mapMonster.MapY,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        Position = Session.Character.Direction,
                        IsMoving = mapMonster.IsMoving,
                        MapMonsterId = ServerManager.Instance.GetNextMobId()
                    };
                    if (!DAOFactory.MapMonsterDAO.DoesMonsterExist(backMonst.MapMonsterId))
                    {
                        DAOFactory.MapMonsterDAO.Insert(backMonst);
                        if (DAOFactory.MapMonsterDAO.LoadById(backMonst.MapMonsterId) is MapMonsterDTO monsterDTO)
                        {
                            var monster = new MapMonster(monsterDTO);
                            monster.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddMonster(monster);
                            Session.CurrentMapInstance?.Broadcast(monster.GenerateIn());
                            Session.CurrentMapInstance.RemovedMobNpcList.Remove(mapMonster);
                            Session.SendPacket(Session.Character.GenerateSay(
                                $"MapMonster VNum: {backMonst.MonsterVNum} recovered sucessfully", 10));
                        }
                    }
                }
                else if (lastObject is MapNpc mapNpc)
                {
                    var backNpc = new MapNpcDTO
                    {
                        NpcVNum = mapNpc.NpcVNum,
                        MapX = mapNpc.MapX,
                        MapY = mapNpc.MapY,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        Position = Session.Character.Direction,
                        IsMoving = mapNpc.IsMoving,
                        MapNpcId = ServerManager.Instance.GetNextMobId()
                    };
                    if (!DAOFactory.MapNpcDAO.DoesNpcExist(backNpc.MapNpcId))
                    {
                        DAOFactory.MapNpcDAO.Insert(backNpc);
                        if (DAOFactory.MapNpcDAO.LoadById(backNpc.MapNpcId) is MapNpcDTO npcDTO)
                        {
                            var npc = new MapNpc(npcDTO);
                            npc.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddNPC(npc);
                            Session.CurrentMapInstance?.Broadcast(npc.GenerateIn());
                            Session.CurrentMapInstance.RemovedMobNpcList.Remove(mapNpc);
                            Session.SendPacket(
                                Session.Character.GenerateSay($"MapNpc VNum: {backNpc.NpcVNum} recovered sucessfully",
                                    10));
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BackMobPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}