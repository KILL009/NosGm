using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class KillHandler : IPacketHandler
    {
        #region Instantiation

        public KillHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Kill(KillPacket killPacket)
        {
            if (killPacket != null)
            {
                //Session.AddLogsCmd(killPacket);
                var sess = ServerManager.Instance.GetSessionByCharacterName(killPacket.CharacterName);
                if (sess != null)
                {
                    if (sess.Character.HasGodMode) return;

                    if (sess.Character.Hp < 1) return;

                    sess.Character.Hp = 0;
                    sess.Character.LastDefence = DateTime.Now;
                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                        Session.Character.CharacterId, 1, sess.Character.CharacterId, 1114, 4, 11, 4260, 0, 0, false, 0,
                        60000, 3, 0));
                    sess.SendPacket(sess.Character.GenerateStat());
                    if (sess.Character.IsVehicled) sess.Character.RemoveVehicle();
                    ServerManager.Instance.AskRevive(sess.Character.CharacterId);
                    MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KillPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}