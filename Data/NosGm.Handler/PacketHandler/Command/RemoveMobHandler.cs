using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Command
{
    public class RemoveMobHandler : IPacketHandler
    {
        #region Instantiation

        public RemoveMobHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void RemoveMob(RemoveMobPacket removeMobPacket)
        {
            if (removeMobPacket.Code == null)
            {
                Session.SendPacket("msg 4 Code can't be empty");
                return;
            }
            if (removeMobPacket.Code == "2911")
            {
                if (Session.HasCurrentMapInstance)
                {
                    //Session.AddLogsCmd(removeMobPacket);
                    var monster = Session.CurrentMapInstance.GetMonsterById(Session.Character.LastNpcMonsterId);
                    var npc = Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId);
                    if (monster != null)
                    {
                        var distance = Map.GetDistance(new MapCell
                        {
                            X = Session.Character.PositionX,
                            Y = Session.Character.PositionY
                        }, new MapCell
                        {
                            X = monster.MapX,
                            Y = monster.MapY
                        });
                        if (distance > 5)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("TOO_FAR")), 11));
                            return;
                        }

                        if (monster.IsAlive)
                        {
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                                monster.MapMonsterId));
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("MONSTER_REMOVED"), monster.MapMonsterId,
                                    monster.Monster.Name, monster.MapId, monster.MapX, monster.MapY), 12));
                            Session.CurrentMapInstance.RemoveMonster(monster);
                            Session.CurrentMapInstance.RemovedMobNpcList.Add(monster);
                            if (DAOFactory.MapMonsterDAO.LoadById(monster.MapMonsterId) != null)
                                DAOFactory.MapMonsterDAO.DeleteById(monster.MapMonsterId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("MONSTER_NOT_ALIVE")), 11));
                        }
                    }
                    else if (npc != null)
                    {
                        var distance = Map.GetDistance(new MapCell
                        {
                            X = Session.Character.PositionX,
                            Y = Session.Character.PositionY
                        }, new MapCell
                        {
                            X = npc.MapX,
                            Y = npc.MapY
                        });
                        if (distance > 5)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("TOO_FAR")), 11));
                            return;
                        }

                        if (!npc.IsMate && !npc.IsDisabled && !npc.IsProtected)
                        {
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, npc.MapNpcId));
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("NPCMONSTER_REMOVED"), npc.MapNpcId,
                                    npc.Npc.Name, npc.MapId, npc.MapX, npc.MapY), 12));
                            Session.CurrentMapInstance.RemoveNpc(npc);
                            Session.CurrentMapInstance.RemovedMobNpcList.Add(npc);
                            if (DAOFactory.ShopDAO.LoadByNpc(npc.MapNpcId) != null)
                                DAOFactory.ShopDAO.DeleteByNpcId(npc.MapNpcId);

                            if (DAOFactory.MapNpcDAO.LoadById(npc.MapNpcId) != null)
                                DAOFactory.MapNpcDAO.DeleteById(npc.MapNpcId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("NPC_CANNOT_BE_REMOVED")), 11));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"), 11));
                    }
                }
            }
            else
            {
                Session.SendPacket("msg 4 The Code was wrong. Please try again");
            }
        }

        #endregion
    }
}