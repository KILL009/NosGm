using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.Handler.PacketHandler.ScriptedInstance
{
    public class WreqPacketHandler : IPacketHandler
    {
        #region Instantiation

        public WreqPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GetWreq(WreqPacket packet)
        {
            var CharPositionX = Session.Character.PositionX;
            var CharPositionY = Session.Character.PositionY;
            foreach (var portal in Session.CurrentMapInstance.ScriptedInstances)
            {
                if (CharPositionY >= portal.PositionY - 1 && CharPositionY
                                                          <= portal.PositionY + 1
                                                          && CharPositionX
                                                          >= portal.PositionX - 1
                                                          && CharPositionX
                                                          <= portal.PositionX + 1)
                {
                    switch (packet.Value)
                    {
                        case 0:
                            if (packet.Param != 1
                             && Session.Character.Group?.Sessions.Find(s =>
                                        s.CurrentMapInstance.InstanceBag?.Lock == false
                                     && s.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                                     && s.Character.CharacterId != Session.Character.CharacterId
                                     && s.Character.Timespace?.Id == portal.Id
                                     && !s.Character.Timespace.IsIndividual) is ClientSession TeamMemberInInstance)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#wreq^3^{TeamMemberInInstance.Character.CharacterId} #wreq^0^1 {Language.Instance.GetMessageFromKey("ASK_JOIN_TEAM_TS")}"));
                            }
                            else
                            {
                                Session.SendPacket(portal.GenerateRbr(Session.Character));
                            }

                            break;

                        case 1:
                            if (!packet.Param.HasValue)
                            {
                                Session.Character.EnterInstance(portal);
                            }

                            break;

                        case 3:
                            var clientSession =
                                    Session.Character.Group?.Sessions.Find(s => s.Character.CharacterId == packet.Param);
                            if (clientSession != null && clientSession.CurrentMapInstance.InstanceBag?.Lock == false &&
                                clientSession.Character?.Timespace is GameObject.ScriptedInstance TeamTimeSpace &&
                                !TeamTimeSpace.IsIndividual)
                            {
                                if (portal.Id == TeamTimeSpace.Id)
                                {
                                    if (Session.Character.Level < TeamTimeSpace.LevelMinimum)
                                    {
                                        Session.SendPacket(
                                                UserInterfaceHelper.GenerateMsg(
                                                        Language.Instance.GetMessageFromKey("TOO_LOW_LVL"), 0));
                                        return;
                                    }

                                    if (Session.Character.Level > TeamTimeSpace.LevelMaximum)
                                    {
                                        Session.SendPacket(
                                                UserInterfaceHelper.GenerateMsg(
                                                        Language.Instance.GetMessageFromKey("TOO_HIGH_LVL"), 0));
                                        return;
                                    }

                                    if (TeamTimeSpace.DailyEntries == 0)
                                    {
                                        foreach (var gift in TeamTimeSpace.RequiredItems)
                                        {
                                            if (Session.Character.Inventory.CountItem(gift.VNum) < gift.Amount)
                                            {
                                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                        string.Format(
                                                                Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"),
                                                                ServerManager.GetItem(gift.VNum).Name), 0));
                                                return;
                                            }

                                            Session.Character.Inventory.RemoveItemAmount(gift.VNum, gift.Amount);
                                        }

                                        Session?.SendPackets(TeamTimeSpace.GenerateMinimap());
                                        Session?.SendPacket(TeamTimeSpace.GenerateMainInfo());
                                        Session?.SendPacket(TeamTimeSpace.FirstMap.InstanceBag.GenerateScore());
                                        if (TeamTimeSpace.StartX != 0 || TeamTimeSpace.StartY != 0)
                                        {
                                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                    clientSession.CurrentMapInstance.MapInstanceId, TeamTimeSpace.StartX,
                                                    TeamTimeSpace.StartY);
                                        }
                                        else
                                        {
                                            ServerManager.Instance.TeleportOnRandomPlaceInMap(Session,
                                                    clientSession.CurrentMapInstance.MapInstanceId);
                                        }

                                        Session.Character.Timespace = TeamTimeSpace;
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 0));
                                        Session.SendPacket(Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 10));
                                    }
                                }
                            }
                            else
                            {
                                GetWreq(new WreqPacket { Value = 0, Param = 1 });
                            }

                            // TODO: Implement
                            break;
                    }
                }
            }
        }

        #endregion
    }
}