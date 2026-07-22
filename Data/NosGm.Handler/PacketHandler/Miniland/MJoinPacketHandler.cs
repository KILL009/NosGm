using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Miniland
{
    public class MJoinPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MJoinPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void JoinMiniland(MJoinPacket mJoinPacket)
        {
            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance ||
                Session.Character.IsSeal)
            {
                var sess = ServerManager.Instance.GetSessionByCharacterId(mJoinPacket.CharacterId);
                if (sess?.Character != null && (ServerManager.Instance.ChannelId != 51 ||
                                                sess.Character.Faction == Session.Character.Faction))
                {
                    //if (!Session.Character.IsFriendOfCharacter(sess.Character.CharacterId) && !sess.Character
                    //        .BattleEntity.GetOwnedNpcs()
                    //        .Any(s => sess.Character.BattleEntity.IsSignpost(s.NpcVNum)))
                    //{
                    //    return;
                    //}

                    if (sess.Character.MinilandState == MinilandState.Open)
                    {
                        ServerManager.Instance.JoinMiniland(Session, sess);
                    }
                    else
                    {
                        Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(
                                        Language.Instance.GetMessageFromKey("MINILAND_CLOSED_BY_FRIEND")));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_THAT"),
                    10));
            }
        }

        #endregion
    }
}