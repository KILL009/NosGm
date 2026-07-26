using Game.Configuration;
using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NosGm.GameObject
{
    public class BCard : BCardDTO
    {
        public BCard()
        {
        }

        public BCard(BCardDTO input) : this()
        {
            BCardId = input.BCardId;
            CardId = input.CardId;
            CastType = input.CastType;
            FirstData = input.FirstData;
            IsLevelDivided = input.IsLevelDivided;
            IsLevelScaled = input.IsLevelScaled;
            ItemVNum = input.ItemVNum;
            NpcMonsterVNum = input.NpcMonsterVNum;
            SecondData = input.SecondData;
            SkillVNum = input.SkillVNum;
            SubType = input.SubType;
            ThirdData = input.ThirdData;
            Type = input.Type;
        }

        #region Properties

        public bool IsPartnerSkillBCard { get; set; }

        public int ForceDelay { get; set; }

        #endregion Properties

        #region Methods

        public void ApplyBCards(BattleEntity target, BattleEntity caster, short x = 0, short y = 0,
            short partnerBuffLevel = 0, short levelUpgraded = 0,
            BCardExecutionPhase executionPhase = BCardExecutionPhase.Unspecified,
            SkillCastContext castContext = null, HitContext hitContext = null)
        {
            int firstData = FirstData;
            int casterLevel = caster.MapMonster?.Owner?.Level ?? caster.Level;

            Card card = null;
            Skill skill = null;
            int delayTime = 0;
            int duration = 0;

            if (CardId is short cardId2 && ServerManager.Instance.GetCardByCardId(cardId2) is Card BuffCard)
            {
                card = BuffCard;

                if (CastType == 1)
                {
                    delayTime = card.Delay * 100;
                }

                duration = card.Duration * 100 - delayTime;
            }

            if (SkillVNum is short skillVNum && ServerManager.GetSkill(skillVNum) is Skill Skill)
            {
                skill = Skill;
                if (caster.Character != null)
                {
                    List<CharacterSkill> skills = caster.Character.GetSkills();

                    if (skills != null)
                    {
                        firstData = skills.Find(s => s.SkillVNum == skill.SkillVNum)?.GetSkillBCards()
                            .OrderByDescending(s => s.SkillVNum)
                            .FirstOrDefault(b => b.Type == Type && b.SubType == SubType)?.FirstData ?? FirstData;
                        if (firstData == 0)
                        {
                            firstData = FirstData;
                        }
                    }
                }
            }

            if (ForceDelay > 0)
            {
                delayTime = ForceDelay * 100;
            }

            int disposableKey = skill?.SkillVNum == 1098 ? skill.SkillVNum * 1000 : BCardId;
            if (BCardId > 0)
            {
                target.BCardDisposables[disposableKey]?.Dispose();
            }

            target.BCardDisposables[disposableKey] =
                Observable.Timer(TimeSpan.FromMilliseconds(delayTime)).Subscribe(o =>
                {
                    if (SpecialDamageAndExplosionsRuntime.TryRegisterPersistentRule(
                            target,
                            this,
                            firstData,
                            executionPhase))
                    {
                        BCardPipelineMonitor.RecordPassiveSkipped();
                        return;
                    }

                    PluginFacility.HandleBCard(new BCardEvent
                    {
                        Target = target,
                        Caster = caster,
                        Card = card,
                        BCard = this,
                        LevelUpgraded = levelUpgraded,
                        X = x,
                        Y = y,
                        Skill = skill,
                        FirstData = firstData,
                        CasterLevel = casterLevel,
                        DelayTime = delayTime,
                        Duration = duration,
                        ExecutionPhase = executionPhase,
                        CastContext = castContext,
                        HitContext = hitContext
                    });
                });
        }

        #endregion Methods
    }

    /// <summary>
    /// Executes the two Type 29 shapes backed by the modern combat reference:
    /// SurroundingsExplosion (21) and SurroundingsAttack (31).
    ///
    /// The rows observed in NosGM belong to active cards, so they are registered as bounded
    /// offensive rules and evaluated from the structured damage event. The uncertain negated
    /// subtype 12 deliberately remains in the normal missing-handler diagnostics.
    /// </summary>
    internal static class SpecialDamageAndExplosionsRuntime
    {
        private const byte SurroundingsExplosion = 21;
        private const byte SurroundingsAttack = 31;
        private const int MaximumRegisteredRules = 4096;
        private const int MaximumAreaTargets = 50;
        private const int MaximumRadius = 15;
        private const int MaximumFixedDamage = 1000000;

        private static readonly ConcurrentDictionary<string, OffensiveRule> Rules =
            new ConcurrentDictionary<string, OffensiveRule>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<byte, byte> LoggedActivations =
            new ConcurrentDictionary<byte, byte>();

        static SpecialDamageAndExplosionsRuntime()
        {
            CombatDamageDiagnostics.CalculationCompleted += OnDamageCalculated;
        }

        public static bool TryRegisterPersistentRule(
            BattleEntity owner,
            BCard bcard,
            int effectiveFirstData,
            BCardExecutionPhase executionPhase)
        {
            if (owner == null || bcard == null || bcard.BCardId <= 0 || !bcard.CardId.HasValue ||
                bcard.Type != (byte)BCardType.CardType.SpecialDamageAndExplosions)
            {
                return false;
            }

            if (bcard.SubType != SurroundingsExplosion && bcard.SubType != SurroundingsAttack)
            {
                return false;
            }

            // These samples are persistent card effects. Direct skill BCards are not registered here,
            // because attaching a permanent rule to one skill activation would leak its effect.
            if (executionPhase != BCardExecutionPhase.Unspecified &&
                executionPhase != BCardExecutionPhase.BuffApply)
            {
                return false;
            }

            string key = BuildKey(owner, bcard.BCardId);
            IDisposable previous = owner.BCardDisposables?[bcard.BCardId];
            previous?.Dispose();

            if (Rules.Count >= MaximumRegisteredRules && !Rules.ContainsKey(key))
            {
                RemoveStaleRules();
                if (Rules.Count >= MaximumRegisteredRules)
                {
                    Logger.Warn(
                        $"[SPECIAL_DAMAGE_RULE_LIMIT] Limit={MaximumRegisteredRules} " +
                        $"Owner={(short)owner.UserType}:{owner.MapEntityId} BCardId={bcard.BCardId}");
                    return true;
                }
            }

            Rules[key] = new OffensiveRule(
                key,
                owner,
                bcard.BCardId,
                bcard.SubType,
                effectiveFirstData,
                bcard.SecondData);

            owner.BCardDisposables[bcard.BCardId] = new RuleRegistration(key);
            return true;
        }

        private static void OnDamageCalculated(DamageCalculationResult result)
        {
            if (result == null || result.FinalDamage <= 0 || result.HitMode == 2 || result.HitMode == 4)
            {
                return;
            }

            foreach (KeyValuePair<string, OffensiveRule> pair in Rules.ToArray())
            {
                OffensiveRule rule = pair.Value;
                BattleEntity owner = rule?.Owner?.Target as BattleEntity;
                if (owner?.MapInstance == null)
                {
                    Rules.TryRemove(pair.Key, out _);
                    continue;
                }

                if (owner.MapEntityId != result.AttackerId || owner.UserType != result.AttackerUserType)
                {
                    continue;
                }

                BattleEntity defender = owner.MapInstance.BattleEntities.FirstOrDefault(entity =>
                    entity != null &&
                    entity.MapEntityId == result.DefenderId &&
                    entity.UserType == result.DefenderUserType);

                if (defender == null || defender == owner || defender.MapInstance != owner.MapInstance)
                {
                    continue;
                }

                Guid triggerId = result.CastId ?? result.HitId ?? result.CalculationId;
                if (!rule.TryMarkTrigger(triggerId))
                {
                    continue;
                }

                switch (rule.SubType)
                {
                    case SurroundingsExplosion:
                        ProcessSurroundingsExplosion(owner, defender, rule);
                        break;

                    case SurroundingsAttack:
                        ProcessSurroundingsAttack(owner, defender, result, rule);
                        break;
                }
            }
        }

        private static void ProcessSurroundingsExplosion(
            BattleEntity owner,
            BattleEntity originalDefender,
            OffensiveRule rule)
        {
            int radius = ClampRadius(rule.FirstData);
            int damage = ClampFixedDamage(rule.SecondData);
            if (radius <= 0 || damage <= 0)
            {
                return;
            }

            int affected = ApplyAreaDamage(
                owner,
                owner,
                radius,
                damage,
                excludedTarget: null);

            LogFirstActivation(SurroundingsExplosion, affected, radius, damage, originalDefender);
        }

        private static void ProcessSurroundingsAttack(
            BattleEntity owner,
            BattleEntity originalDefender,
            DamageCalculationResult result,
            OffensiveRule rule)
        {
            if (result.TargetHitType != TargetHitType.SingleTargetHit &&
                result.TargetHitType != TargetHitType.SingleTargetHitCombo)
            {
                return;
            }

            int chance = ClampPercent(rule.FirstData);
            int radius = ClampRadius(rule.SecondData);
            if (chance <= 0 || radius <= 0 || chance < 100 && ServerManager.RandomNumber() >= chance)
            {
                return;
            }

            int affected = ApplyAreaDamage(
                owner,
                originalDefender,
                radius,
                result.FinalDamage,
                originalDefender);

            LogFirstActivation(SurroundingsAttack, affected, radius, result.FinalDamage, originalDefender);
        }

        private static int ApplyAreaDamage(
            BattleEntity owner,
            BattleEntity center,
            int radius,
            int rawDamage,
            BattleEntity excludedTarget)
        {
            if (owner?.MapInstance == null || center == null || rawDamage <= 0)
            {
                return 0;
            }

            List<BattleEntity> candidates = owner.MapInstance.BattleEntities
                .Where(entity => entity != null &&
                                 entity != owner &&
                                 entity != excludedTarget &&
                                 entity.MapInstance == owner.MapInstance &&
                                 entity.Hp > 0 &&
                                 center.GetDistance(entity) <= radius)
                .Take(MaximumAreaTargets)
                .ToList();

            int affected = 0;
            foreach (BattleEntity candidate in candidates)
            {
                bool canAttack;
                try
                {
                    canAttack = owner.CanAttackEntity(candidate);
                }
                catch
                {
                    canAttack = false;
                }

                if (!canAttack)
                {
                    continue;
                }

                // Secondary area damage remains non-lethal until monster/player death processing is
                // centralized. This prevents zero-HP entities that bypass their normal reward/events path.
                int appliedDamage = candidate.GetDamage(rawDamage, owner, dontKill: true);
                if (appliedDamage <= 0)
                {
                    continue;
                }

                candidate.MapInstance?.Broadcast(candidate.GenerateDm(appliedDamage));
                candidate.Character?.Session?.SendPacket(candidate.Character.GenerateStat());
                affected++;
            }

            return affected;
        }

        private static void LogFirstActivation(
            byte subType,
            int affected,
            int radius,
            int damage,
            BattleEntity originalDefender)
        {
            if (affected <= 0 || !LoggedActivations.TryAdd(subType, 0))
            {
                return;
            }

            Logger.Info(
                $"[SPECIAL_DAMAGE_ACTIVE] SubType={subType} Affected={affected} Radius={radius} " +
                $"Damage={damage} Defender={(short)originalDefender.UserType}:{originalDefender.MapEntityId}");
        }

        private static void RemoveStaleRules()
        {
            foreach (KeyValuePair<string, OffensiveRule> pair in Rules.ToArray())
            {
                if (!(pair.Value?.Owner?.Target is BattleEntity owner) || owner.MapInstance == null)
                {
                    Rules.TryRemove(pair.Key, out _);
                }
            }
        }

        private static string BuildKey(BattleEntity owner, int bcardId) =>
            $"{RuntimeHelpers.GetHashCode(owner)}:{bcardId}";

        private static int ClampPercent(int value)
        {
            long absolute = Math.Abs((long)value);
            return absolute >= 100 ? 100 : (int)absolute;
        }

        private static int ClampRadius(int value)
        {
            long absolute = Math.Abs((long)value);
            if (absolute <= 0)
            {
                return 0;
            }

            return absolute >= MaximumRadius ? MaximumRadius : (int)absolute;
        }

        private static int ClampFixedDamage(int value)
        {
            long absolute = Math.Abs((long)value);
            if (absolute <= 0)
            {
                return 0;
            }

            return absolute >= MaximumFixedDamage ? MaximumFixedDamage : (int)absolute;
        }

        private sealed class OffensiveRule
        {
            private readonly object _triggerLock = new object();
            private Guid? _lastTriggerId;

            public OffensiveRule(
                string key,
                BattleEntity owner,
                int bcardId,
                byte subType,
                int firstData,
                int secondData)
            {
                Key = key;
                Owner = new WeakReference(owner);
                BCardId = bcardId;
                SubType = subType;
                FirstData = firstData;
                SecondData = secondData;
            }

            public int BCardId { get; }

            public int FirstData { get; }

            public string Key { get; }

            public WeakReference Owner { get; }

            public int SecondData { get; }

            public byte SubType { get; }

            public bool TryMarkTrigger(Guid triggerId)
            {
                lock (_triggerLock)
                {
                    if (_lastTriggerId == triggerId)
                    {
                        return false;
                    }

                    _lastTriggerId = triggerId;
                    return true;
                }
            }
        }

        private sealed class RuleRegistration : IDisposable
        {
            private readonly string _key;
            private int _disposed;

            public RuleRegistration(string key)
            {
                _key = key;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                Rules.TryRemove(_key, out _);
            }
        }
    }
}