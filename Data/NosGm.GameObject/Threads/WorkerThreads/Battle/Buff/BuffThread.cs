using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject.Threads.WorkerThreads.Battle.Buff
{
    public static class BuffThread
    {
        public static void AddGroupBuff(ClientSession session)
        {
            if (session.Character.Group != null && session.Character.Group.GroupType == GroupType.Group)
            {
                switch (session.Character.Group.SessionCount)
                {
                    case 2:
                        if (!session.Character.HasBuff(5000))
                        {
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5000 });
                            session.Character.RemoveBuff(5001);
                        }
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 837));
                        session.Character.LastGroupEffect = DateTime.Now;
                        break;

                    case 3:
                        if (!session.Character.HasBuff(5001))
                        {
                            session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 5001 });
                            session.Character.RemoveBuff(5000);
                        }
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 838));
                        session.Character.LastGroupEffect = DateTime.Now;
                        break;
                }
            }
        }

        public static void RemoveGroupBuff(ClientSession session)
        {
            if (session.Character.Group != null)
            {
                switch (session.Character.Group.SessionCount)
                {
                    case 2:
                        session.Character.RemoveBuff(5001);
                        break;

                    case 3:
                        session.Character.RemoveBuff(5000);
                        break;
                }
            }
            else
            {
                session.Character.RemoveBuff(5000);
                session.Character.RemoveBuff(5001);
            }
        }

    }
}
