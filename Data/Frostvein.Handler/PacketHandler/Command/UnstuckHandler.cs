using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class UnstuckHandler : IPacketHandler
    {
        #region Instantiation

        public UnstuckHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Unstuck(UnstuckPacket unstuckPacket)
        {
            var time = Session.Character.LastCMD.AddSeconds(10);
            //Session.AddLogsCmd(unstuckPacket);
            if (DateTime.Now <= time) // Anti spam
                return;
            Session.Character.LastCMD = DateTime.Now;

            if (Session?.Character != null)
            {
                if (Session.Character.MapId == 9998)
                {
                    Session.SendPacket("msg 4 You can't use that command there.");
                    return;
                }
                if (Session.Character.Miniland == Session.Character.MapInstance)
                {
                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else if (!Session.Character.IsSeal && !Session.CurrentMapInstance.MapInstanceType.Equals(MapInstanceType.TalentArenaMapInstance) && !Session.CurrentMapInstance.MapInstanceType.Equals(MapInstanceType.IceBreakerInstance))
                {
                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                        Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                        true);
                    Session.SendPacket(StaticPacketHelper.Cancel(2));
                }
            }
        }

        #endregion
    }
}