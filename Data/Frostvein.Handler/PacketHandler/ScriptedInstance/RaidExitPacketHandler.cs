using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
{
    public class RaidExitPacketHandler : IPacketHandler
    {
        #region Instantiation

        public RaidExitPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void InstanceExit(RaidExitPacket rxitPacket)
        {
            if (rxitPacket?.State == 1)
            {
                if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance && Session.Character.Timespace != null)
                {
                    if (Session.CurrentMapInstance.InstanceBag.Lock)
                    {
                        //5 seeds
                        if (Session.Character.Inventory.CountItem(1012) >= 5)
                        {
                            Session.CurrentMapInstance.InstanceBag.DeadList.Add(Session.Character.CharacterId);
                            Session.Character.Dignity -= 50;
                            if (Session.Character.Dignity < -1000)
                            {
                                Session.Character.Dignity = -1000;
                            }
                            Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_LOST"), 50), 11));
                            Session.Character.Inventory.RemoveItemAmount(1012, 5);
                        }
                        if (Session.Character.Inventory.CountItem(1012) < 1)
                        {
                            Session.Character.Dignity -= 80;
                            if (Session.Character.Dignity < -1000)
                            {
                                Session.Character.Dignity = -1000;
                            }

                            Session.Character.Reputation -= 500;
                            if (Session.Character.Reputation < 1) //stupid condition but who knows.
                            {
                                Session.Character.Reputation = 0;
                            }

                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MORE_DIGNITY_LOST"), 80), 11));
                        }
                    }

                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);

                }

                if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    if (Session.CurrentMapInstance.InstanceBag.Lock)
                    {
                        //5 seeds
                        if (Session.Character.Inventory.CountItem(1012) >= 5)
                        {
                            Session.CurrentMapInstance.InstanceBag.DeadList.Add(Session.Character.CharacterId);
                            Session.Character.Dignity -= 50;
                            if (Session.Character.Dignity < -1000)
                            {
                                Session.Character.Dignity = -1000;
                            }
                            Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_LOST"), 50), 11));
                            Session.Character.Inventory.RemoveItemAmount(1012, 5);
                        }

                    }
                    else if (Session.Character.Inventory.CountItem(1012) < 1) //Same goes for raid but more dignity/rep lost
                    {
                        Session.Character.Dignity -= 100;
                        if (Session.Character.Dignity < -1000)
                        {
                            Session.Character.Dignity = -1000;
                        }

                        Session.Character.Reputation -= 1000;
                        if (Session.Character.Reputation < 1) //stupid condition but who knows.
                        {
                            Session.Character.Reputation = 0;
                        }
                        Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MORE_DIGNITY_LOST"), 80), 11));
                    }
                    ServerManager.Instance.GroupLeave(Session);
                }
                else if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                {
                    Session.Character.LeaveTalentArena(true);
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, ServerManager.Instance.ArenaInstance.MapInstanceId);

                }
            }
        }

        #endregion
    }
}