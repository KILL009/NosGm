using Game.Configuration.BCards;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.GameObject.Battle
{
    public class HitRequest
    {
        #region Instantiation

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, NpcMonsterSkill skill,
            SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill?.Skill.Effect ?? 0;
            CastContext = castContext ?? CreateCastContext(mate?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, Skill skill,
            SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.BCards ?? new List<BCard>();
            SkillEffect = skill?.Effect ?? 0;
            CastContext = castContext ?? CreateCastContext(mate?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, MapMonster monster, NpcMonsterSkill skill,
            bool showTargetAnimation = false, SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Monster = monster;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill?.Skill.Effect ?? 0;
            ShowTargetHitAnimation = showTargetAnimation;
            CastContext = castContext ?? CreateCastContext(monster?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Skill skill, short? skillEffect = null,
            short? mapX = null, short? mapY = null, ComboDTO skillCombo = null, bool showTargetAnimation = false,
            List<BCard> skillBCards = null, int directDamage = 0, SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Session = session;
            Skill = skill;
            TargetHitType = targetHitType;
            SkillEffect = skillEffect ?? skill?.Effect ?? 0;
            ShowTargetHitAnimation = showTargetAnimation;
            DirectDamage = directDamage;

            if (mapX.HasValue) MapX = mapX.Value;

            if (mapY.HasValue) MapY = mapY.Value;

            if (skillCombo != null) SkillCombo = skillCombo;

            if (skillBCards != null)
                SkillBCards = skillBCards;
            else
                SkillBCards = skill?.BCards ?? new List<BCard>();

            CastContext = castContext ?? CreateCastContext(session?.Character?.BattleEntity, Skill);
        }

        #endregion

        #region Properties

        public SkillCastContext CastContext { get; set; }

        public int DirectDamage { get; }

        public DateTime HitTimestamp { get; set; }

        public HitContext LastHitContext { get; private set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public Mate Mate { get; set; }

        public MapMonster Monster { get; set; }

        public NpcMonsterSkill NpcMonsterSkill { get; set; }

        public ClientSession Session { get; set; }

        /// <summary>
        ///     Some AOE Skills need to show additional SU packet for Animation
        /// </summary>
        public bool ShowTargetHitAnimation { get; set; }

        public Skill Skill { get; set; }

        public List<BCard> SkillBCards { get; set; }

        public ComboDTO SkillCombo { get; set; }

        public short SkillEffect { get; set; }

        public TargetHitType TargetHitType { get; set; }

        #endregion

        /// <summary>
        /// Executes the legacy damage formula through the structured adapter. New combat routes
        /// should use this method so every target keeps its cast, hit and result metadata together.
        /// </summary>
        public DamageCalculationResult CalculateDamage(BattleEntity attacker, BattleEntity defender,
            ref int hitMode, ref bool onyxWings, ref bool zephyrWings, ref bool dragonBuff,
            int hitIndex = 0, bool attackGreaterDistance = false)
        {
            LastHitContext = CreateHitContext(defender, hitIndex);
            return DamageHelper.Instance.CalculateDamageDetailed(attacker, defender, Skill, LastHitContext,
                ref hitMode, ref onyxWings, ref zephyrWings, ref dragonBuff, attackGreaterDistance);
        }

        /// <summary>
        /// Executes one BCard with the cast and hit metadata belonging to this request.
        /// This is the only supported entry point for BCards fired by a resolved combat impact.
        /// </summary>
        public void ApplyBCard(BCard bcard, BattleEntity target, BattleEntity caster,
            BCardExecutionPhase executionPhase, short x = 0, short y = 0,
            short partnerBuffLevel = 0, short levelUpgraded = 0)
        {
            if (bcard == null || target == null || caster == null)
            {
                return;
            }

            HitContext hitContext = LastHitContext;
            if (hitContext == null &&
                (executionPhase == BCardExecutionPhase.Hit ||
                 executionPhase == BCardExecutionPhase.ReceiveHit ||
                 executionPhase == BCardExecutionPhase.Kill))
            {
                hitContext = CreateHitContext(target);
                LastHitContext = hitContext;
            }

            bcard.ApplyBCards(target, caster, x, y, partnerBuffLevel, levelUpgraded,
                executionPhase, CastContext, hitContext);
        }

        public HitContext CreateHitContext(BattleEntity target, int hitIndex = 0)
        {
            return new HitContext
            {
                CastContext = CastContext,
                Target = target,
                HitIndex = hitIndex,
                TargetHitType = TargetHitType
            };
        }

        private static SkillCastContext CreateCastContext(BattleEntity caster, Skill skill)
        {
            short originX = caster?.PositionX ?? 0;
            short originY = caster?.PositionY ?? 0;
            return SkillCastContext.Create(caster, skill, originX, originY,
                caster?.Character != null && skill != null && caster.Character.MapInstance != null &&
                caster.Character.MapInstance.MapInstanceType == MapInstanceType.PVPInstance);
        }
    }

    /// <summary>
    /// Non-invasive adapter around the legacy DamageHelper. It gives new combat routes a structured
    /// result without changing the proven legacy formula while that formula is split into stages.
    /// </summary>
    public static class DamageHelperStructuredExtensions
    {
        private const short AchillesUltimateSkillVNum = 1961;
        private const byte DistanceDamagePerCellSubtype = 21;
        private const int AchillesUltimatePercentPerSynchronizationLevel = 10;

        public static DamageCalculationResult CalculateDamageDetailed(this DamageHelper helper,
            BattleEntity attacker, BattleEntity defender, Skill skill, HitContext hitContext,
            ref int hitMode, ref bool onyxWings, ref bool zephyrWings, ref bool dragonBuff,
            bool attackGreaterDistance = false)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            DateTime startedAtUtc = DateTime.UtcNow;
            int legacyDamage = helper.CalculateDamage(attacker, defender, skill, ref hitMode,
                ref onyxWings, ref zephyrWings, ref dragonBuff, attackGreaterDistance);

            ModernSpecialistSnapshot attackerSnapshot =
                AuthoritativeModernSpecialistStateStore.GetSnapshot(attacker?.Character);
            int attackerSynchronizationLevel = attackerSnapshot.SynchronizationLevel;
            int synchronizationAttackPercent = attackerSnapshot.AttackPercentPerSynchronizationLevel *
                                               attackerSynchronizationLevel;
            int synchronizationAttackBonus = CalculatePercentage(legacyDamage, synchronizationAttackPercent);

            int heroAttackPercent = 0;
            Character heroOwner = attacker?.MapMonster?.Owner?.Character;
            if (heroOwner != null)
            {
                ModernSpecialistSnapshot heroOwnerSnapshot =
                    AuthoritativeModernSpecialistStateStore.GetSnapshot(heroOwner);
                heroAttackPercent = heroOwnerSnapshot.HeroAttackPercent;
            }

            int heroAttackBonus = CalculatePercentage(legacyDamage, heroAttackPercent);

            int distancePercentPerCell = GetDistanceDamagePercentPerCell(attacker);
            int distanceCells = GetDistanceCells(attacker, defender);
            int distanceDamagePercent = ClampDamage((long)distancePercentPerCell * distanceCells);
            int distanceDamageBonus = CalculatePercentage(legacyDamage, distanceDamagePercent);

            int generalAttackBonus = ClampDamage(
                (long)synchronizationAttackBonus + heroAttackBonus + distanceDamageBonus);
            int damageAfterGeneralBonuses = ClampDamage((long)legacyDamage + generalAttackBonus);

            int ultimateSynchronizationPercent = skill?.SkillVNum == AchillesUltimateSkillVNum
                ? attackerSynchronizationLevel * AchillesUltimatePercentPerSynchronizationLevel
                : 0;
            int ultimateSynchronizationBonus =
                CalculatePercentage(damageAfterGeneralBonuses, ultimateSynchronizationPercent);
            int damageBeforeDefence =
                ClampDamage((long)damageAfterGeneralBonuses + ultimateSynchronizationBonus);

            ModernSpecialistSnapshot defenderSnapshot =
                AuthoritativeModernSpecialistStateStore.GetSnapshot(defender?.Character);
            int defenderSynchronizationLevel = defenderSnapshot.SynchronizationLevel;
            int synchronizationDefencePercent = defenderSnapshot.DefencePercentPerSynchronizationLevel *
                                                defenderSynchronizationLevel;
            int synchronizationDefenceReduction =
                CalculatePercentage(damageBeforeDefence, synchronizationDefencePercent);
            int finalDamage = ClampDamage((long)damageBeforeDefence - synchronizationDefenceReduction);
            DateTime completedAtUtc = DateTime.UtcNow;

            int totalAttackBonus = ClampDamage((long)generalAttackBonus + ultimateSynchronizationBonus);
            if (totalAttackBonus != 0 || synchronizationDefenceReduction != 0)
            {
                Logger.Info(
                    $"[SP_SYNC_DAMAGE] CastId={FormatCastId(hitContext)} " +
                    $"SkillVNum={(skill == null ? "-" : skill.SkillVNum.ToString())} " +
                    $"Attacker={attacker?.MapEntityId ?? 0} Defender={defender?.MapEntityId ?? 0} " +
                    $"Legacy={legacyDamage} SyncAttackLevel={attackerSynchronizationLevel} " +
                    $"SyncAttackPercent={synchronizationAttackPercent} " +
                    $"SyncAttackBonus={synchronizationAttackBonus} " +
                    $"HeroAttackPercent={heroAttackPercent} HeroAttackBonus={heroAttackBonus} " +
                    $"DistanceCells={distanceCells} DistancePercentPerCell={distancePercentPerCell} " +
                    $"DistanceDamagePercent={distanceDamagePercent} DistanceDamageBonus={distanceDamageBonus} " +
                    $"UltimateSyncPercent={ultimateSynchronizationPercent} " +
                    $"UltimateSyncBonus={ultimateSynchronizationBonus} " +
                    $"DefenderSyncLevel={defenderSynchronizationLevel} " +
                    $"SyncDefencePercent={synchronizationDefencePercent} " +
                    $"SyncDefenceReduction={synchronizationDefenceReduction} Final={finalDamage}");
            }

            var result = new DamageCalculationResult
            {
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                CastId = hitContext?.CastContext?.CastId,
                HitId = hitContext?.HitId,
                AttackerId = attacker?.MapEntityId ?? 0,
                AttackerUserType = attacker != null ? attacker.UserType : default(UserType),
                DefenderId = defender?.MapEntityId ?? 0,
                DefenderUserType = defender != null ? defender.UserType : default(UserType),
                SkillVNum = skill?.SkillVNum,
                TargetHitType = hitContext?.TargetHitType ?? default(TargetHitType),
                FinalDamage = finalDamage,
                HitMode = hitMode,
                OnyxWings = onyxWings,
                ZephyrWings = zephyrWings,
                DragonBuff = dragonBuff,
                AttackGreaterDistance = attackGreaterDistance,
                IsPvp = hitContext?.CastContext?.IsPvp ??
                        (attacker?.Character != null && defender?.Character != null),
                Breakdown = new DamageBreakdown
                {
                    LegacyDamage = legacyDamage,
                    SynchronizationAttackBonus = synchronizationAttackBonus == 0
                        ? (int?)null
                        : synchronizationAttackBonus,
                    HeroAttackBonus = heroAttackBonus == 0 ? (int?)null : heroAttackBonus,
                    DistanceDamageBonus = distanceDamageBonus == 0 ? (int?)null : distanceDamageBonus,
                    SynchronizationUltimateBonus = ultimateSynchronizationBonus == 0
                        ? (int?)null
                        : ultimateSynchronizationBonus,
                    BonusDamage = totalAttackBonus == 0 ? (int?)null : totalAttackBonus,
                    DamageReduction = synchronizationDefenceReduction == 0
                        ? (int?)null
                        : synchronizationDefenceReduction,
                    FinalDamage = finalDamage,
                    IsComplete = false
                }
            };

            if (hitContext != null)
            {
                // RawDamage is the proven legacy result. FinalDamage includes the modern
                // specialist synchronization and Achilles-specific modifiers applied here.
                hitContext.RawDamage = legacyDamage;
                hitContext.FinalDamage = finalDamage;
                hitContext.HitMode = hitMode;
                hitContext.DamageResult = result;
            }

            CombatDamageDiagnostics.Publish(result);
            return result;
        }

        private static int GetDistanceDamagePercentPerCell(BattleEntity attacker)
        {
            if (attacker?.BCards == null)
            {
                return 0;
            }

            return attacker.BCards
                .Where(bcard => bcard != null &&
                                bcard.Type == (byte)BCardType.CardType.HideBarrelSkill &&
                                bcard.SubType == DistanceDamagePerCellSubtype)
                .Select(bcard => Math.Abs(bcard.FirstData))
                .DefaultIfEmpty(0)
                .Max();
        }

        private static int GetDistanceCells(BattleEntity attacker, BattleEntity defender)
        {
            if (attacker == null || defender == null)
            {
                return 0;
            }

            return Map.GetDistance(
                new MapCell { X = attacker.PositionX, Y = attacker.PositionY },
                new MapCell { X = defender.PositionX, Y = defender.PositionY });
        }

        private static int CalculatePercentage(int value, int percent)
        {
            if (value <= 0 || percent <= 0)
            {
                return 0;
            }

            return ClampDamage((long)value * percent / 100);
        }

        private static int ClampDamage(long value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static string FormatCastId(HitContext hitContext) =>
            hitContext?.CastContext == null ? "-" : hitContext.CastContext.CastId.ToString("N");
    }

    /// <summary>
    /// Structured output of one damage calculation. During the migration the legacy formula fills
    /// the final values, while later phases will populate each individual component of Breakdown.
    /// </summary>
    public sealed class DamageCalculationResult
    {
        public Guid CalculationId { get; set; } = Guid.NewGuid();

        public Guid? CastId { get; set; }

        public Guid? HitId { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime CompletedAtUtc { get; set; }

        public long AttackerId { get; set; }

        public UserType AttackerUserType { get; set; }

        public long DefenderId { get; set; }

        public UserType DefenderUserType { get; set; }

        public short? SkillVNum { get; set; }

        public TargetHitType TargetHitType { get; set; }

        public int FinalDamage { get; set; }

        public int HitMode { get; set; }

        public bool OnyxWings { get; set; }

        public bool ZephyrWings { get; set; }

        public bool DragonBuff { get; set; }

        public bool AttackGreaterDistance { get; set; }

        public bool IsPvp { get; set; }

        public DamageBreakdown Breakdown { get; set; } = new DamageBreakdown();

        public double ElapsedMilliseconds => (CompletedAtUtc - StartedAtUtc).TotalMilliseconds;

        public string PipelineVersion => "legacy-adapter-v3-achilles";
    }

    /// <summary>
    /// Explicit damage stages. Nullable values mean the legacy formula has not exposed that stage yet.
    /// This prevents zero from being mistaken for a measured component.
    /// </summary>
    public sealed class DamageBreakdown
    {
        public int? LegacyDamage { get; set; }

        public int? SynchronizationAttackBonus { get; set; }

        public int? HeroAttackBonus { get; set; }

        public int? DistanceDamageBonus { get; set; }

        public int? SynchronizationUltimateBonus { get; set; }

        public int? BaseDamage { get; set; }

        public int? SkillDamage { get; set; }

        public int? AttackContribution { get; set; }

        public int? DefenceReduction { get; set; }

        public int? ElementDamage { get; set; }

        public int? CriticalDamage { get; set; }

        public int? BonusDamage { get; set; }

        public int? DamageReduction { get; set; }

        public int FinalDamage { get; set; }

        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Optional observer for diagnostics and tests. Listener failures are isolated from combat.
    /// </summary>
    public static class CombatDamageDiagnostics
    {
        public static event Action<DamageCalculationResult> CalculationCompleted;

        public static bool HasSubscribers => CalculationCompleted != null;

        internal static void Publish(DamageCalculationResult result)
        {
            Action<DamageCalculationResult> handlers = CalculationCompleted;
            if (handlers == null || result == null)
            {
                return;
            }

            foreach (Action<DamageCalculationResult> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(result);
                }
                catch (Exception exception)
                {
                    Logger.Error("[COMBAT_DAMAGE_DIAGNOSTIC_FAILED] A damage observer failed.", exception);
                }
            }
        }
    }
}
