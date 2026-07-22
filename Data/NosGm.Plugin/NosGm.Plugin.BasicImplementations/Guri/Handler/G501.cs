using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Event;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G501 : IGuriHandler
    {
        public long GuriEffectId => 501;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 501)
            {
                if (ServerManager.Instance.IceBreakerInWaiting && IceBreaker.Map.Sessions.Count() < IceBreaker.MaxAllowedPlayers
                                                               && Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && Session.Character.Group?.Raid == null)
                {
                    if (Session.Character.Gold >= 500)
                    {
                        Session.Character.Gold -= 500;
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.Character.RemoveVehicle();
                        ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, IceBreaker.Map.MapInstanceId);
                        var NewIceTeam = new ConcurrentBag<ClientSession>();
                        NewIceTeam.Add(Session);
                        IceBreaker.IceBreakerTeams.Add(NewIceTeam);
                    }
                    else
                    {
                        Session.SendPacket(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"));
                    }
                }
            }
        }
    }
}