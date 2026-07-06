using Frostvein.Algorithm;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Items
{
    public static class VNum11011
    {
        public static async Task Execute(ClientSession Session)
        {
            if (Session.Character.IsExchanging) { Session.SendPacket("info You cannot do that while Trading"); return; }
            if (Session.Character.Channel.ChannelId == 51) { Session.SendPacket("info You cannot do that in Glacernon."); return; }
            if (Session.Character.IsShopping) { Session.SendPacket("info You cannot do that while Shopping"); return; }
            if (Session.Character.IsSeal) { Session.SendPacket("info You cannot do that while being in a Raid"); return; }
            if (Session.Character.Group != null && Session.Character.Group.GroupType == GroupType.Group) { Session.SendPacket("info You cannot do that while being in a Group"); return; }

            if (Session.Character.LastInstanceCreated.AddSeconds(30) <= DateTime.Now)
            {
                if (Session.Character.IsCurrentlyOnCustomMapInstance)
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.InstanceMapId, Session.Character.InstanceMapX, Session.Character.InstanceMapY);
                    Session.Character.IsCurrentlyOnCustomMapInstance = false;
                    Session.Character.LastInstanceCreated = DateTime.Now;
                    MessageExtension.SendGrey(Session, "You left your Private Map Instance");
                }
                else
                {
                    Session.Character.InstanceMapId = Session.Character.MapId;
                    Session.Character.InstanceMapX = Session.Character.MapX;
                    Session.Character.InstanceMapY = Session.Character.MapY;
                    MapInstance map = null;
                    map = ServerManager.GenerateMapInstance(Session.Character.MapId, MapInstanceType.CustomInstance, new InstanceBag());
                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, map.MapInstanceId, Session.Character.MapX, Session.Character.MapY);
                    Session.Character.CustomInstance = map;
                    Session.Character.IsCurrentlyOnCustomMapInstance = true;
                    Session.Character.LastInstanceCreated = DateTime.Now;
                    MessageExtension.SendGrey(Session, "Your Private Map Instance has been created");

                }
            }
            else
            {
                Session.SendPacket("info You have to wait 30 Seconds before doing that again");
            }
        }
    }
}
