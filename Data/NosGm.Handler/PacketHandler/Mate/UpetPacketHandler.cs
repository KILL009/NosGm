using NosGm.Packets.Packets.ClientPackets;
using NosGm.Extension.Extension.Packet;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace NosGm.Handler.PacketHandler.Mate
{
    public class UpetPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UpetPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task SpecialSkillAsync(UpetPacket upetPacket)
        {
            if (upetPacket == null || Session.Character == null)
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

            // u_pet identifies the exact pet that owns the skill. Selecting the
            // first active pet made cooldown state leak to a different companion.
            Mate attacker = Session.Character.Mates.Find(mate =>
                mate.MateTransportId == upetPacket.MateTransportId &&
                mate.IsTeamMember &&
                mate.IsAlive &&
                mate.MateType == MateType.Pet);

            if (attacker?.BattleEntity == null ||
                attacker.Monster == null ||
                attacker.IsSitting)
            {
                return;
            }

            // PSkills contains per-mate NpcMonsterSkill instances. Using the shared
            // Monster.Skills collection made LastSkillUse global across pets that
            // use the same monster template.
            NpcMonsterSkill mateSkill = attacker.PSkills?
                .FirstOrDefault(candidate =>
                    candidate?.Skill != null &&
                    MateHelper.Instance.PetSkills.Contains(candidate.SkillVNum));

            if (mateSkill?.Skill == null ||
                !mateSkill.CanBeUsed() ||
                Session.Character.npcMonstersSkillsInCd.Contains(mateSkill.SkillVNum))
            {
                return;
            }

            BattleEntity battleEntityDefender = ResolveTarget(upetPacket);
            if (battleEntityDefender == null ||
                battleEntityDefender.MapInstance != attacker.BattleEntity.MapInstance ||
                battleEntityDefender.Hp <= 0)
            {
                return;
            }

            Skill skill = PartnerSkillHelper.ConvertToNormalPSkill(mateSkill);
            if (skill == null)
            {
                return;
            }

            int cooldownMilliseconds = Math.Max(0, mateSkill.Skill.Cooldown * 100);
            Character owner = Session.Character;

            // PetSkillTargetHit currently owns damage/cast execution, while this
            // handler owns the per-pet cooldown and the client cooldown indicator.
            mateSkill.LastSkillUse = DateTime.Now;
            owner.npcMonstersSkillsInCd.Add(mateSkill.SkillVNum);

            Observable.Timer(TimeSpan.FromMilliseconds(cooldownMilliseconds)).Subscribe(_ =>
            {
                owner.npcMonstersSkillsInCd.Remove(mateSkill.SkillVNum);
            });
            Session.SendPacketAfter("petsr 0", cooldownMilliseconds);

            Logger.Debug(
                $"[MATE_COMBAT] Source=UPET Action=Special Mate={attacker.MateTransportId} " +
                $"Skill={mateSkill.SkillVNum} TargetType={battleEntityDefender.UserType} " +
                $"Target={battleEntityDefender.MapEntityId}");

            MateExt.PetSkillTargetHit(attacker.BattleEntity, battleEntityDefender, skill);
        }

        private BattleEntity ResolveTarget(UpetPacket upetPacket)
        {
            switch (upetPacket.TargetType)
            {
                case UserType.Player:
                    return Session.CurrentMapInstance?
                        .GetCharacterById(upetPacket.TargetId)?
                        .BattleEntity;

                case UserType.Npc:
                    return Session.CurrentMapInstance?
                        .GetMate(upetPacket.TargetId)?
                        .BattleEntity;

                case UserType.Monster:
                    return Session.CurrentMapInstance?
                        .GetMonsterById(upetPacket.TargetId)?
                        .BattleEntity;

                default:
                    return null;
            }
        }

        #endregion
    }
}
