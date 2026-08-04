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
using GameMate = NosGm.GameObject.Mate;

namespace NosGm.Handler.PacketHandler.Mate
{
    public class UpetPacketHandler : IPacketHandler
    {
        private const int DefaultSushiPartyTauntDurationMilliseconds = 10000;

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
            GameMate attacker = Session.Character.Mates.Find(mate =>
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

            Logger.Info(
                $"[MATE_COMBAT] Source=UPET Action=Special Mate={attacker.MateTransportId} " +
                $"Npc={attacker.NpcMonsterVNum} Skill={mateSkill.SkillVNum} " +
                $"TargetType={battleEntityDefender.UserType} Target={battleEntityDefender.MapEntityId} " +
                $"SkillTargetType={skill.TargetType} HitType={skill.HitType} " +
                $"Range={skill.Range} TargetRange={skill.TargetRange} " +
                $"BCards={skill.BCards?.Count ?? 0}");

            MateExt.PetSkillTargetHit(attacker.BattleEntity, battleEntityDefender, skill);
            ApplyPositiveOwnerBuffs(attacker, battleEntityDefender, skill);
            ApplyAreaAttraction(attacker, skill);
        }

        private void ApplyAreaAttraction(GameMate attacker, Skill skill)
        {
            // Fiesta de sushi (663) is a self-centred support/taunt skill. The
            // packet/BCard path applies its buffs, but it never transferred monster
            // aggro to the pet. That left the visible "attract nearby enemies"
            // description without a server-side effect.
            if (skill?.SkillVNum != 663 ||
                skill.TargetRange <= 0 ||
                attacker?.BattleEntity == null ||
                Session.CurrentMapInstance == null)
            {
                return;
            }

            var monsters = Session.CurrentMapInstance
                .GetMonsterInRangeList(
                    attacker.PositionX,
                    attacker.PositionY,
                    skill.TargetRange)?
                .Where(monster =>
                    monster?.BattleEntity != null &&
                    monster.IsAlive &&
                    monster.CurrentHp > 0 &&
                    monster.MapInstance == attacker.BattleEntity.MapInstance &&
                    attacker.BattleEntity.CanAttackEntity(monster.BattleEntity))
                .ToList();

            if (monsters == null || monsters.Count == 0)
            {
                Logger.Info(
                    $"[MATE_TAUNT] Mate={attacker.MateTransportId} Skill={skill.SkillVNum} " +
                    $"Range={skill.TargetRange} Attracted=0");
                return;
            }

            int durationMilliseconds = ResolveAttractionDurationMilliseconds(skill, attacker.Level);
            foreach (MapMonster monster in monsters)
            {
                BattleEntity previousTarget = monster.Target;
                bool petWasAlreadyAggroed = monster.AggroList?.Any(candidate =>
                    IsSameEntity(candidate, attacker.BattleEntity)) == true;

                monster.AddToAggroList(attacker.BattleEntity);
                monster.Target = attacker.BattleEntity;
                monster.LastMonsterAggro = DateTime.Now;

                ScheduleAttractionRelease(
                    monster,
                    attacker.BattleEntity,
                    previousTarget,
                    petWasAlreadyAggroed,
                    durationMilliseconds);
            }

            Logger.Info(
                $"[MATE_TAUNT] Mate={attacker.MateTransportId} Skill={skill.SkillVNum} " +
                $"Range={skill.TargetRange} DurationMs={durationMilliseconds} " +
                $"Attracted={monsters.Count}");
        }

        private static int ResolveAttractionDurationMilliseconds(Skill skill, int petLevel)
        {
            int durationDeciseconds = skill.BCards?
                .Where(card => card.Type == (byte)BCardType.CardType.Buff)
                .Select(card => new Buff((short)card.SecondData, petLevel).Card?.Duration ?? 0)
                .Where(duration => duration > 0)
                .DefaultIfEmpty(DefaultSushiPartyTauntDurationMilliseconds / 100)
                .Max() ?? DefaultSushiPartyTauntDurationMilliseconds / 100;

            return Math.Max(1000, durationDeciseconds * 100);
        }

        private static void ScheduleAttractionRelease(
            MapMonster monster,
            BattleEntity pet,
            BattleEntity previousTarget,
            bool petWasAlreadyAggroed,
            int durationMilliseconds)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(durationMilliseconds).ConfigureAwait(false);

                try
                {
                    if (monster?.BattleEntity == null ||
                        monster.MapInstance == null ||
                        !monster.IsAlive ||
                        pet == null)
                    {
                        return;
                    }

                    bool petIsStillValid = pet.Hp > 0 && pet.MapInstance == monster.MapInstance;
                    bool petHasDamageAggro = monster.DamageList?.Keys.Any(candidate =>
                        IsSameEntity(candidate, pet)) == true;
                    bool keepNaturalAggro = petIsStillValid &&
                                            (petWasAlreadyAggroed || petHasDamageAggro);

                    if (!keepNaturalAggro)
                    {
                        monster.RemoveFromAggroList(pet);
                    }

                    if (!IsSameEntity(monster.Target, pet))
                    {
                        return;
                    }

                    BattleEntity restoredTarget = IsValidMonsterTarget(monster, previousTarget)
                        ? previousTarget
                        : monster.AggroList?.FirstOrDefault(candidate =>
                            !IsSameEntity(candidate, pet) &&
                            IsValidMonsterTarget(monster, candidate));

                    if (restoredTarget != null)
                    {
                        monster.Target = restoredTarget;
                    }
                    else if (!keepNaturalAggro)
                    {
                        monster.Target = null;
                    }

                    Logger.Info(
                        $"[MATE_TAUNT] Mate={pet.MapEntityId} Monster={monster.MapMonsterId} " +
                        $"Result=Released RestoredTarget={monster.Target?.MapEntityId ?? 0} " +
                        $"NaturalAggro={keepNaturalAggro}");
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        exception,
                        $"[MATE_TAUNT] Mate={pet?.MapEntityId ?? 0} " +
                        $"Monster={monster?.MapMonsterId ?? 0} Result=ReleaseFailed");
                }
            });
        }

        private static bool IsValidMonsterTarget(MapMonster monster, BattleEntity target)
        {
            return monster?.BattleEntity != null &&
                   target != null &&
                   target.Hp > 0 &&
                   target.MapInstance == monster.MapInstance &&
                   monster.BattleEntity.CanAttackEntity(target);
        }

        private static bool IsSameEntity(BattleEntity left, BattleEntity right)
        {
            return left != null &&
                   right != null &&
                   left.MapEntityId == right.MapEntityId &&
                   left.EntityType == right.EntityType;
        }

        private void ApplyPositiveOwnerBuffs(
            GameMate attacker,
            BattleEntity battleEntityDefender,
            Skill skill)
        {
            if (attacker?.BattleEntity == null ||
                battleEntityDefender == null ||
                Session.Character?.BattleEntity == null ||
                skill?.BCards == null)
            {
                return;
            }

            bool targetsPetItself =
                battleEntityDefender.MapEntityId == attacker.MateTransportId &&
                battleEntityDefender.UserType == UserType.Npc;

            // Fiesta de sushi and similar skills use TargetType=1/HitType=1 but
            // target the pet itself. Their good/neutral BCards must also reach the
            // owner. Enemy-directed offensive skills remain excluded.
            bool isSupportLayout = skill.TargetType == 2 ||
                                   skill.TargetType == 1 &&
                                   (skill.HitType != 1 || targetsPetItself);
            if (!isSupportLayout)
            {
                Logger.Info(
                    $"[MATE_BUFF] Owner={Session.Character.CharacterId} " +
                    $"Mate={attacker.MateTransportId} Skill={skill.SkillVNum} " +
                    "Result=SkippedNonSupportLayout");
                return;
            }

            foreach (BCard bcard in skill.BCards.Where(card =>
                         card.Type == (byte)BCardType.CardType.Buff))
            {
                Buff buff = new Buff((short)bcard.SecondData, attacker.Level);
                if (buff.Card == null ||
                    buff.Card.BuffType != BuffType.Good &&
                    buff.Card.BuffType != BuffType.Neutral)
                {
                    continue;
                }

                if (Session.Character.Buff.Any(existing =>
                        existing.Card?.CardId == buff.Card.CardId))
                {
                    Logger.Info(
                        $"[MATE_BUFF] Owner={Session.Character.CharacterId} " +
                        $"Mate={attacker.MateTransportId} Skill={skill.SkillVNum} " +
                        $"Card={buff.Card.CardId} Result=AlreadyActive");
                    continue;
                }

                bcard.ApplyBCards(
                    Session.Character.BattleEntity,
                    attacker.BattleEntity);
                Logger.Info(
                    $"[MATE_BUFF] Owner={Session.Character.CharacterId} " +
                    $"Mate={attacker.MateTransportId} Skill={skill.SkillVNum} " +
                    $"Card={buff.Card.CardId} Result=AppliedToOwner");
            }
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
