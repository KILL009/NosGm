using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using System;

namespace Game.Configuration.BCards
{
    /// <summary>
    /// Describes the point of the combat pipeline in which a BCard is being evaluated.
    /// Unspecified keeps backwards compatibility with callers that have not been migrated yet.
    /// </summary>
    public enum BCardExecutionPhase : byte
    {
        Unspecified = 0,
        Cast = 1,
        Hit = 2,
        ReceiveHit = 3,
        Kill = 4,
        BuffApply = 5,
        BuffRemove = 6,
        Periodic = 7
    }

    /// <summary>
    /// Separates values consumed by the damage/stat calculators from executable effects.
    /// </summary>
    public enum BCardExecutionKind : byte
    {
        Executable = 0,
        PassiveCalculation = 1,
        Hybrid = 2
    }

    /// <summary>
    /// Shared state for every target produced by one skill activation.
    /// </summary>
    public sealed class SkillCastContext
    {
        public Guid CastId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public BattleEntity Caster { get; set; }

        public Skill Skill { get; set; }

        public short OriginX { get; set; }

        public short OriginY { get; set; }

        public bool IsPvp { get; set; }

        public static SkillCastContext Create(BattleEntity caster, Skill skill, short originX, short originY,
            bool isPvp = false)
        {
            return new SkillCastContext
            {
                CastId = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                Caster = caster,
                Skill = skill,
                OriginX = originX,
                OriginY = originY,
                IsPvp = isPvp
            };
        }
    }

    /// <summary>
    /// Per-target state belonging to a SkillCastContext.
    /// Damage values are populated progressively by the combat pipeline.
    /// </summary>
    public sealed class HitContext
    {
        public Guid HitId { get; set; } = Guid.NewGuid();

        public SkillCastContext CastContext { get; set; }

        public BattleEntity Target { get; set; }

        public int HitIndex { get; set; }

        public TargetHitType TargetHitType { get; set; }

        public int RawDamage { get; set; }

        public int FinalDamage { get; set; }

        public int HitMode { get; set; }

        public bool IsCritical { get; set; }

        public DamageCalculationResult DamageResult { get; set; }
    }

    /// <summary>
    /// Conservative classification used while the legacy combat routes are unified.
    /// Only types that are consumed as values by stat/damage code and have no action lifecycle
    /// are marked as calculation-only. Hybrid types must still reach their registered handler.
    /// </summary>
    public static class BCardExecutionClassifier
    {
        private const byte DistanceDamagePerCellSubtype = 21;

        public static BCardExecutionKind GetKind(BCardType.CardType type, byte subtype)
        {
            if (type == BCardType.CardType.StealBuff)
            {
                switch (subtype)
                {
                    case (byte)AdditionalTypes.StealBuff.IgnoreDefenceChance:
                    case (byte)AdditionalTypes.StealBuff.IgnoreDefenceChanceNegated:
                    case (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChance:
                    case (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChanceNegated:
                    case (byte)AdditionalTypes.StealBuff.ChanceSummonOnyxDragon:
                    case (byte)AdditionalTypes.StealBuff.ChanceSummonOnyxDragonNegated:
                        return BCardExecutionKind.PassiveCalculation;
                    default:
                        return BCardExecutionKind.Executable;
                }
            }

            // Modern SP12 data reuses the old HideBarrelSkill numeric type for the
            // Festering Curse distance-based damage modifier. It is consumed by the
            // structured damage adapter and has no executable lifecycle of its own.
            if (type == BCardType.CardType.HideBarrelSkill && subtype == DistanceDamagePerCellSubtype)
            {
                return BCardExecutionKind.PassiveCalculation;
            }

            return GetKind(type);
        }

        public static BCardExecutionKind GetKind(BCardType.CardType type)
        {
            switch (type)
            {
                case BCardType.CardType.AttackPower:
                case BCardType.CardType.RecoveryAndDamagePercent:
                    return BCardExecutionKind.Hybrid;

                case BCardType.CardType.SpecialAttack:
                case BCardType.CardType.SpecialDefence:
                case BCardType.CardType.Target:
                case BCardType.CardType.Critical:
                case BCardType.CardType.SpecialCritical:
                case BCardType.CardType.Element:
                case BCardType.CardType.IncreaseDamage:
                case BCardType.CardType.Defence:
                case BCardType.CardType.DodgeAndDefencePercent:
                case BCardType.CardType.Block:
                case BCardType.CardType.Absorption:
                case BCardType.CardType.ElementResistance:
                case BCardType.CardType.EnemyElementResistance:
                case BCardType.CardType.Damage:
                case BCardType.CardType.GuarantedDodgeRangedAttack:
                case BCardType.CardType.Morale:
                case BCardType.CardType.Casting:
                case BCardType.CardType.CalculatingLevel:
                case BCardType.CardType.MaxHPMP:
                case BCardType.CardType.MultAttack:
                case BCardType.CardType.MultDefence:
                case BCardType.CardType.NoCharacteristicValue:
                case BCardType.CardType.DebuffResistance:
                case BCardType.CardType.FairyXPIncrease:
                case BCardType.CardType.ElementalBonusDamage:
                case BCardType.CardType.IncreaseResistanceByPercent:
                case BCardType.CardType.IncreaseDamageVsEntity:
                case BCardType.CardType.IncreaseDamageVsFaction:
                case BCardType.CardType.IncreaseDamageVsMonsterInMap:
                case BCardType.CardType.IncreaseSlPoint:
                case BCardType.CardType.IncreaseDamageVsChar:
                case BCardType.CardType.IncreaseHpMp:
                    return BCardExecutionKind.PassiveCalculation;

                default:
                    return BCardExecutionKind.Executable;
            }
        }

        public static bool IsPassiveCalculationOnly(BCardType.CardType type, byte subtype) =>
            GetKind(type, subtype) == BCardExecutionKind.PassiveCalculation;

        public static bool IsPassiveCalculationOnly(BCardType.CardType type) =>
            GetKind(type) == BCardExecutionKind.PassiveCalculation;
    }

    public class BCardEvent
    {
        #region Properties

        public BCard BCard { get; set; }

        public Card? Card { get; set; }

        public int LevelUpgraded { get; set; }

        public BattleEntity Target { get; set; }

        public BattleEntity Caster { get; set; }

        public Skill? Skill { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        public int FirstData { get; set; }

        public int CasterLevel { get; set; }

        public int DelayTime { get; set; }

        public int Duration { get; set; }

        public BCardExecutionPhase ExecutionPhase { get; set; }

        public SkillCastContext CastContext { get; set; }

        public HitContext HitContext { get; set; }

        #endregion Properties
    }
}
