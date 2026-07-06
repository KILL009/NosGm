using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frostvein.GameObject.Plugin.Event;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class PreqPacketHandler : IPacketHandler
    {
        #region Instantiation

        public PreqPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Preq(PreqPacket packet)
        {
            if (Session.Character.IsSeal)
            {
                return;
            }

            var currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;

            var timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;

            if (Session.Character.EditorMode)
            {
                MessageExtension.SendGreen(Session, "You are currently in the Editor Mode. Action was blocked.");
                return;
            }

            if (timeSpanSinceLastPortal < 4 || !Session.HasCurrentMapInstance)
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                return;
            }

            if (Session.CurrentMapInstance.Portals.Concat(Session.Character.GetExtraPortal())
                .FirstOrDefault(s => Session.Character.PositionY >= s.SourceY - 1
                                     && Session.Character.PositionY <= s.SourceY + 1
                                     && Session.Character.PositionX >= s.SourceX - 1
                                     && Session.Character.PositionX <= s.SourceX + 1) is Portal portal)
            {
                switch (portal.Type)
                {

                    case (sbyte)PortalType.Exit:
                        if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            Session?.CurrentMapInstance.Dispose();
                            EventHelper.Instance.RunEvent(new EventContainer(Session?.CurrentMapInstance, EventActionType.DISPOSEMAP, null));
                            Session.Character.IsOnMapInstance = false;
                        }
                        break;


                    case (sbyte)PortalType.MapPortal:
                    case (sbyte)PortalType.TSNormal:
                    case (sbyte)PortalType.Open:
                    case (sbyte)PortalType.Miniland:
                    case (sbyte)PortalType.TSEnd:
                    case (sbyte)PortalType.Effect:
                    case (sbyte)PortalType.ShopTeleport:
                        break;

                    case (sbyte)PortalType.Raid:
                        if (Session.Character.Group?.Raid != null)
                        {
                            if (Session.Character.Group.IsLeader(Session))
                            {
                                 Session.SendPacket($"qna #mkraid^0^275 {Language.Instance.GetMessageFromKey("RAID_START_QUESTION")}");
                            }
                            else
                            {
                                 Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_TEAM_LEADER"), 10));
                            }
                        }
                        else
                        {
                             Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NEED_TEAM"), 10));
                        }

                        return;

                    case (sbyte)PortalType.BlueRaid:
                    case (sbyte)PortalType.DarkRaid:
                        if (!packet.Parameter.HasValue)
                        {
                             Session.SendPacket($"qna #preq^1 {string.Format(Language.Instance.GetMessageFromKey("ACT4_RAID_ENTER"), Session.Character.Level * 5)}");
                            return;
                        }
                        else
                        {
                            if (packet.Parameter == 1)
                            {
                                if ((int)Session.Character.Faction == portal.Type - 9 && Session.Character.Family?.Act4Raid != null)
                                {
                                    if (Session.Character.Level > 49)
                                    {
                                        if (Session.Character.Reputation > 10000)
                                        {
                                            Session.Character.GetReputation(Session.Character.Level * -20); //-5

                                            Session.Character.LastPortal = currentRunningSeconds;

                                            switch (Session.Character.Family.Act4Raid.MapInstanceType)
                                            {
                                                case MapInstanceType.Act4Morcos:
                                                    ServerManager.Instance.ChangeMapInstance(
                                                        Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 43, 179);
                                                    break;

                                                case MapInstanceType.Act4Hatus:
                                                    ServerManager.Instance.ChangeMapInstance(
                                                        Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 15, 9);
                                                    break;

                                                case MapInstanceType.Act4Calvina:
                                                    ServerManager.Instance.ChangeMapInstance(
                                                        Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 24, 6);
                                                    break;

                                                case MapInstanceType.Act4Berios:
                                                    ServerManager.Instance.ChangeMapInstance(
                                                        Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 20, 20);
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                             Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_REP"), 10));
                                        }
                                    }
                                    else
                                    {
                                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_LVL"), 10));
                                    }
                                }
                                else
                                {
                                     Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));
                                }
                            }
                        }

                        return;

                    default:
                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));
                        return;
                }

                if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance && Session?.Character?.Timespace != null && !Session.Character.Timespace.InstanceBag.Lock)
                {
                    if (Session.Character.CharacterId == Session.Character.Timespace.InstanceBag.CreatorId)
                    {
                         Session.SendPacket(UserInterfaceHelper.GenerateDialog($"#rstart^1 rstart {Language.Instance.GetMessageFromKey("FIRST_ROOM_START")}"));
                    }

                    return;
                }

                if (portal.SourceX == 37 && portal.SourceY == 69 && portal.SourceMapId == 2106)
                {
                     Session.SendPacket("ascr 0 0 0 0 0 0 0 0 -1");
                    Session.Character.CurrentArenaDeath = 0;
                    Session.Character.CurrentArenaKill = 0;
                }

                //TUNDRA PORTAL 
                if (ServerManager.Instance.ChannelId == 51)
                {
                    if ((Session.Character.Faction == FactionType.Angel && portal.DestinationMapId == 131) ||
                        (Session.Character.Faction == FactionType.Demon && portal.DestinationMapId == 130) ||
                        (Session.Character.Faction == FactionType.Angel && portal.SourceX == 135 && portal.SourceY == 171 && portal.DestinationMapId == 134) ||
                        (Session.Character.Faction == FactionType.Angel && portal.SourceX == 128 && portal.SourceY == 174 && portal.DestinationMapId == 153) ||
                        (Session.Character.Faction == FactionType.Angel && portal.SourceX == 110 && portal.SourceY == 159 && portal.DestinationMapId == 154) ||
                        (Session.Character.Faction == FactionType.Demon && portal.SourceX == 46 && portal.SourceY == 171 && portal.DestinationMapId == 134) ||
                        (Session.Character.Faction == FactionType.Demon && portal.SourceX == 50 && portal.SourceY == 174 && portal.DestinationMapId == 153) ||
                        (Session.Character.Faction == FactionType.Demon && portal.SourceX == 70 && portal.SourceY == 159 && portal.SourceMapId == 153))
                    {
                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));
                        return;
                    }
                    //bullshit but it works
                    if (Session.Character.Faction == FactionType.Angel && portal.SourceX == 140 && portal.SourceY == 11 && portal.DestinationMapId == 153)
                    {
                        portal.DestinationX = 46;
                        portal.DestinationY = 171;
                    }

                    if (Session.Character.Faction == FactionType.Demon && portal.SourceX == 140 && portal.SourceY == 11 && portal.DestinationMapId == 153)
                    {
                        portal.DestinationX = 135;
                        portal.DestinationY = 171;
                    }

                    if ((portal.DestinationMapId == 130 || portal.DestinationMapId == 131) && timeSpanSinceLastPortal < 10)
                    {
                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                        return;
                    }
                }


                if (portal.LevelRequired != 0)
                    if (Session.Character.Level < portal.LevelRequired)
                    {
                         Session.SendPacket(Session.Character.GenerateSay("Level required to enter: " + portal.LevelRequired + "!", 10));
                         Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("LEVEL_REQUIRED"), 0)));
                        return;
                    }

                if (portal.HeroLevelRequired != 0)
                    if (Session.Character.HeroLevel < portal.HeroLevelRequired)
                    {
                         Session.SendPacket(Session.Character.GenerateSay("Hero-Level required to enter: " + portal.HeroLevelRequired + "!", 10));
                         Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("HERO_LEVEL_REQUIRED"), 0)));
                        return;
                    }

                if (portal.RequiredItem != 0)
                    if (Session.Character.Inventory.GetAllItems().All(i => i.ItemVNum != portal.RequiredItem))
                    {
                         Session.SendPacket(Session.Character.GenerateSay("You need this item in order to enter: " + portal.NomeOggetto + "!", 10));
                         Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("ITEM_REQUIRED_PORTAL"), 0)));
                        return;
                    }

                if (portal.RequiredClass != null)
                    if (Session.Character.Class != (ClassType)portal.RequiredClass)
                    {
                         Session.SendPacket(Session.Character.GenerateSay($"Only {(ClassType)portal.RequiredClass} can enter !", 10));
                         Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("CLASS_REQUIRED_PORTAL"), 0)));
                        return;
                    }

                if (Session?.CurrentMapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance && portal.DestinationMapId == 134)
                    if (!packet.Parameter.HasValue)
                    {
                         Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("ACT4_RAID_EXIT")}");
                        return;
                    }

                switch (Session.CurrentMapInstance.MapInstanceType)
                {
                    case MapInstanceType.GemmeStoneInstance:
                    case MapInstanceType.ArenaInstance:
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                        return;
                }

                portal.OnTraversalEvents.ForEach(e => EventHelper.Instance.RunEvent(e));

                if (portal.DestinationMapInstanceId == default)
                {
                    return;
                }

                if (ServerManager.Instance.ChannelId == 51)
                {
                    if (Session.Character.Faction == FactionType.Angel && portal.DestinationMapId == 131 || Session.Character.Faction == FactionType.Demon && portal.DestinationMapId == 130)
                    {
                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));

                        return;
                    }

                    if ((portal.DestinationMapId == 130 || portal.DestinationMapId == 131) && timeSpanSinceLastPortal < 10)
                    {
                         Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                        return;
                    }
                }

                 Session.SendPacket(Session.CurrentMapInstance.GenerateRsfn());

                if (ServerManager.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                    != MapInstanceType.BaseMapInstance
                    && ServerManager.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                    != MapInstanceType.CaligorInstance
                    && ServerManager.GetMapInstance(portal.DestinationMapInstanceId).MapInstanceType
                    == MapInstanceType.BaseMapInstance)
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                        Session.Character.MapX, Session.Character.MapY);
                }
                else if (portal.DestinationMapInstanceId == Session.Character.Miniland.MapInstanceId)
                {
                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else if (portal.DestinationMapId == 20000)
                {
                    var sess = ServerManager.Instance.Sessions.FirstOrDefault(s =>
                        s.Character.Miniland.MapInstanceId == portal.DestinationMapInstanceId);
                    if (sess != null) ServerManager.Instance.JoinMiniland(Session, sess);
                }
                else
                {
                    if (ServerManager.Instance.ChannelId == 51)
                    {
                        var destinationX = portal.DestinationX;
                        var destinationY = portal.DestinationY;

                        if (portal.DestinationMapInstanceId == CaligorRaid.CaligorMapInstance?.MapInstanceId) /* Caligor Raid Map */
                        {
                            if (!packet.Parameter.HasValue)
                            {
                                 Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("CALIGOR_RAID_ENTER")}");
                                return;
                            }
                        }
                        else if (portal.DestinationMapId == 153) /* Unknown Land */
                        {
                            if (Session.Character.MapInstance == CaligorRaid.CaligorMapInstance && !packet.Parameter.HasValue)
                            {
                                 Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("CALIGOR_RAID_EXIT")}");
                                return;
                            }

                            if (Session.Character.MapInstance != CaligorRaid.CaligorMapInstance)
                            {
                                if (destinationX <= 0 && destinationY <= 0)
                                {
                                    switch (Session.Character.Faction)
                                    {
                                        case FactionType.Angel:
                                            destinationX = 50;
                                            destinationY = 172;
                                            break;

                                        case FactionType.Demon:
                                            destinationX = 130;
                                            destinationY = 172;
                                            break;
                                    }
                                }


                            }

                        }

                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, portal.DestinationMapInstanceId, destinationX, destinationY);
                    }
                    else
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, portal.DestinationMapInstanceId, portal.DestinationX, portal.DestinationY);
                    }
                }

                Session.Character.LastPortal = currentRunningSeconds;
            }
        }

        #endregion
    }
}