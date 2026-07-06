using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Plugin.Event;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G502 : IGuriHandler
    {
        public long GuriEffectId => 502;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 502)
            {
                long? targetId = e.User;

                if (targetId == null)
                {
                    return;
                }

                var target = ServerManager.Instance.GetSessionByCharacterId(targetId.Value);

                if (target?.Character?.MapInstance == null)
                {
                    return;
                }

                if (target.Character.MapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                {
                    if (target.Character.LastPvPKiller == null
                        || target.Character.LastPvPKiller != Session)
                    {
                        IceBreaker.FrozenPlayers.Remove(target);
                        IceBreaker.AlreadyFrozenPlayers.Add(target);
                        target.Character.NoMove = false;
                        target.Character.NoAttack = false;
                        target.SendPacket(target.Character.GenerateCond());
                        target.Character.MapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_UNFROZEN"), target.Character.Name), 0));

                        if (!IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(Session)).Contains(target))
                        {
                            IceBreaker.IceBreakerTeams.Remove(IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(target)));
                            IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(Session)).Add(target);
                        }
                    }
                }
                else
                {
                    target.Character.RemoveBuff(569);
                }
            }
        }
    }
}