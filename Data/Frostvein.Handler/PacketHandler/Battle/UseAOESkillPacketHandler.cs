using Frostvein.Extension.Extension.Packet;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Battle
{
    public class UseAOESkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseAOESkillPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseZonesSkill(UseAOESkillPacket useAoeSkillPacket)
        {
            if (Session?.Character == null || useAoeSkillPacket == null)
            {
                return;
            }

            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                return;
            }

            Session.Character.Direction = Session.Character.BeforeDirection;

            var isMuted = Session.Character.MuteMessage();
            if (isMuted || Session.Character.IsVehicled)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if (Session.Character.LastTransform.AddSeconds(3) > DateTime.Now)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                return;
            }

            int deltaX = Math.Abs(useAoeSkillPacket.MapX - Session.Character.PositionX);
            int deltaY = Math.Abs(useAoeSkillPacket.MapY - Session.Character.PositionY);
            if (deltaX > 8 || deltaY > 8)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(Session.Character.GenerateSay("Action has been logged", 11));
                return;
            }

            if (Session.Character.CanFight && Session.Character.Hp > 0)
            {
                Session.ZoneHit(useAoeSkillPacket.CastId, useAoeSkillPacket.MapX, useAoeSkillPacket.MapY);
            }
        }

        #endregion
    }
}
