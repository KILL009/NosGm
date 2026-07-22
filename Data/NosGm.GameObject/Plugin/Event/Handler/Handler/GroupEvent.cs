using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class GroupEvent
    {
        public static void Load()
        {
            try
            {
                if (ServerManager.Instance.Groups != null)
                {
                    foreach (var grp in ServerManager.Instance.Groups)
                        foreach (var session in grp.Sessions.GetAllItems())
                        {
                            if (grp.GroupType == GroupType.Group)
                            {
                                session.SendPackets(grp.GeneratePst(session));
                            }
                            else if (grp.GroupType == GroupType.Team || grp.GroupType == GroupType.BigTeam || grp.GroupType == GroupType.GiantTeam)
                            {
                                session.SendPacket(grp.GenerateRdlst());
                            }
                            else if (grp.GroupType == GroupType.RBBBlue || grp.GroupType == GroupType.RBBRed)
                            {
                                session.SendPacket(RainbowBattle.RainbowThread.GenerateFbList(session));
                            }
                        }
                }
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }
    }
}
