using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;
using static NosGm.Domain.BCardType;

namespace NosGm.Handler.PacketHandler.Mate
{
    public class SuctlPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SuctlPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Attack(SuctlPacket suctlPacket)
        {
            if (suctlPacket == null)
            {
                return;
            }

            PenaltyLogDTO penalty = Session.Account.PenaltyLogs
                .OrderByDescending(s => s.DateEnd)
                .FirstOrDefault();
            if (Session.Character.IsMuted() && penalty != null)
            {
                string messageKey = Session.Character.Gender == GenderType.Female
                    ? "MUTED_FEMALE"
                    : "MUTED_MALE";

                Session.CurrentMapInstance?.Broadcast(
                    Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(messageKey), 1));
                Session.SendPacket(Session.Character.GenerateSay(
                    string.Format(
                        Language.Instance.GetMessageFromKey("MUTE_TIME"),
                        (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")),
                    11));
                return;
            }

            Mate attacker = Session.Character.Mates.Find(
                mate => mate.MateTransportId == suctlPacket.MateTransportId);
            if (attacker?.BattleEntity == null ||
                attacker.Hp <= 0 ||
                attacker.HasBuff(
                    CardType.SpecialAttack,
                    (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                return;
            }

            BattleEntity target = ResolveTarget(suctlPacket);
            if (target == null)
            {
                return;
            }

            if (!attacker.BattleEntity.CanAttackEntity(target))
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2, target.MapEntityId));
                return;
            }

            // suctl is the normal pet attack command. Pet special skills are handled
            // separately by UpetPacketHandler. Passing a special NpcMonsterSkill here
            // made every basic attack attempt reuse that skill; while it was cooling
            // down, Mate.TargetHit rejected the request and the pet appeared frozen.
            // A null skill deliberately selects Mate.TargetHit's basic-attack path.
            Logger.Debug(
                $"[MATE_COMBAT] Source=SUCTL Action=Basic Mate={attacker.MateTransportId} " +
                $"TargetType={target.UserType} Target={target.MapEntityId}");
            attacker.TargetHit(target, null);
        }

        private BattleEntity ResolveTarget(SuctlPacket suctlPacket)
        {
            switch (suctlPacket.TargetType)
            {
                case UserType.Monster:
                    return Session.CurrentMapInstance?
                        .GetMonsterById(suctlPacket.TargetId)?
                        .BattleEntity;

                case UserType.Npc:
                    return Session.CurrentMapInstance?
                        .GetMate(suctlPacket.TargetId)?
                        .BattleEntity;

                case UserType.Player:
                    return Session.CurrentMapInstance?
                        .GetSessionByCharacterId(suctlPacket.TargetId)?
                        .Character?
                        .BattleEntity;

                case UserType.Object:
                default:
                    return null;
            }
        }

        #endregion
    }
}
