

using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Raid.Threads
{
    public static class RaidStartThread
    {
        public static async Task Run(ClientSession Session)
        {
            if (Session.Character.Group?.Raid != null && Session.Character.Group.IsLeader(Session))
            {
                IEnumerable<ClientSession> duplicateIp = ServerManager.Instance.FindSameIpAddresses(Session.Character.Group.Sessions.GetAllItems());
                if (duplicateIp.Any()) { foreach (var session in duplicateIp) { Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("SAME_IP"), session.Character.Name), 10)); } return; }
                if (Session.Character.Group.SessionCount > (int)Session.Character.Group.GroupType)
                {
                    Session.SendPacket(Session.Character.GenerateSay($"You break the cap {Session.Character.Group.SessionCount}/{(int)Session.Character.Group.GroupType} player limit", 10));
                    return;
                }


                if (Session.Character.Group.SessionCount > 0 || Session.Account.Authority > AuthorityType.GM && Session.Character.Group.Sessions.All(s => s.CurrentMapInstance == Session.CurrentMapInstance))
                {
                    if (Session.Character.Group.Raid.FirstMap == null) Session.Character.Group.Raid.LoadScript(MapInstanceType.RaidInstance, Session.Character);
                    if (Session.Character.Group.Raid.FirstMap == null) return;
                    Session.Character.Group.Raid.InstanceBag.Lock = true;
                    Session.Character.Group.Raid.InstanceBag.Lives = (short)Session.Character.Group.SessionCount;
                    foreach (var session in Session.Character.Group.Sessions.GetAllItems())
                        if (session != null)
                        {
                            ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                session.Character.Group.Raid.FirstMap.MapInstanceId,
                                session.Character.Group.Raid.StartX, session.Character.Group.Raid.StartY);
                            session.SendPacket("raidbf 0 0 25");
                            session.SendPacket(session.Character.Group.GeneraterRaidmbf(session));
                            session.SendPacket(session.Character.GenerateRaid(5));
                            session.SendPacket(session.Character.GenerateRaid(4));
                            session.SendPacket(session.Character.GenerateRaid(3));
                            session.Character.RaidType = (RaidType)Session.Character.Group.Raid.Id;
                        }
                    ServerManager.Instance.GroupList.Remove(Session.Character.Group);
                    //LOGGER await Log.LogAsync($"[RaidThread] A Raid has been started | RaidID: {Session.Character.Group.Raid} | RaidType: {Session.Character.RaidType}");
                }
                else
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RAID_TEAM_NOT_READY"), 0));
                }
            }
        }
    }
}
