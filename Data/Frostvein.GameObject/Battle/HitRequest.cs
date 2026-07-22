using Game.Configuration.BCards;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using System;
using System.Collections.Generic;

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
            int finalDamage = helper.CalculateDamage(attacker, defender, skill, ref hitMode,
                ref onyxWings, ref zephyrWings, ref dragonBuff, attackGreaterDistance);
            DateTime completedAtUtc = DateTime.UtcNow;

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
                    FinalDamage = finalDamage,
                    IsComplete = false
                }
            };

            if (hitContext != null)
            {
                // Until the legacy formula exposes each stage, raw and final are intentionally equal.
                // The IsComplete flag prevents consumers from treating this as a full breakdown.
                hitContext.RawDamage = finalDamage;
                hitContext.FinalDamage = finalDamage;
                hitContext.HitMode = hitMode;
                hitContext.DamageResult = result;
            }

            CombatDamageDiagnostics.Publish(result);
            return result;
        }
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

        public string PipelineVersion => "legacy-adapter-v1";
    }

    /// <summary>
    /// Explicit damage stages. Nullable values mean the legacy formula has not exposed that stage yet.
    /// This prevents zero from being mistaken for a measured component.
    /// </summary>
    public sealed class DamageBreakdown
    {
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
