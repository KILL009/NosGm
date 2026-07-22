using NosGm.Domain;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.GameObject.Extension
{
    public static class TeleportationExtension
    {
       public static void Teleport(ClientSession Session, long Price, short MapId, short MapX, short MapY, int Level)
       {
            if (Session.Character.Channel.ChannelId == 51)
            {
                Session.SendPacket("info Not Possible while beeing in Act4.");
                return;
            }
            if (Session.Character.Level >= Level)
            {
                if (Session.Character.Gold >= Price)
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, MapId, MapX, MapY);
                    Session.Character.Gold -= Price;
                    Session.SendPacket(Session.Character.GenerateGold());
                }
                else
                {
                    Session.SendPacket("info Not enough Gold.");
                }
            }
            else
            {
                Session.SendPacket("info Your Level is not high enough");
            }
       }

        public static void TeleportHeroic(ClientSession Session, long Price, short MapId, short MapX, short MapY, int Level, int HeroLevel)
        {
            if (Session.Character.Channel.ChannelId == 51)
            {
                Session.SendPacket("info Not Possible while beeing in Act4.");
                return;
            }
            if (Session.Character.Level >= Level && Session.Character.HeroLevel >= HeroLevel)
            {
                if (Session.Character.Gold >= Price)
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, MapId, MapX, MapY);
                    Session.Character.Gold -= Price;
                    Session.SendPacket(Session.Character.GenerateGold());
                }
                else
                {
                    Session.SendPacket("info Not enough Gold.");
                }
            }
            else
            {
                Session.SendPacket("info Your Level is not high enough");
            }
        }
    }
}