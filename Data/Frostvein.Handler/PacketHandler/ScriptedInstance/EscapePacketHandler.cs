using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
{
    public class EscapePacketHandler : IPacketHandler
    {
        #region Instantiation

        public EscapePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Escape(EscapePacket escapePacket)
        {
            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                    Session.Character.MapX, Session.Character.MapY);
                Session.Character.Timespace = null;
            }
            else if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
            {
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                    Session.Character.MapX, Session.Character.MapY);
                ServerManager.Instance.GroupLeave(Session);
            }
            else if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.SheepGameInstance)
            {
                int miniscore = 50; // your score
                if (Session.Character.SheepScore1 > miniscore && Session.Character.IsWaitingForGift == true) // Anti Afk to get Reward
                {
                    short[] random1 = { 1, 2, 3 };
                    short[] random = { 2, 4, 6 };
                    short[] acorn = { 5947, 5948, 5949, 5950 };
                    Session.Character.GiftAdd(5951, random1[ServerManager.RandomNumber(0, random1.Length)]);
                    int rnd = ServerManager.RandomNumber(0, 5);
                    switch (rnd)
                    {
                        case 2:
                            Session.Character.GiftAdd(acorn[ServerManager.RandomNumber(0, acorn.Length)], random[ServerManager.RandomNumber(0, random.Length)]);
                            break;
                        default:
                            break;
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                    Session.Character.IsWaitingForGift = false;
                }
            }
        }

        #endregion
    }
}