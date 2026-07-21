using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Game.Configuration.BCards
{
    /// <summary>
    /// Authoritative SP10-SP12 resource state. Handler names intentionally sort before
    /// the provisional handlers in BCardPlugin.cs so the deterministic registry selects
    /// these implementations while the older classes remain available for comparison.
    /// </summary>
    internal static class AuthoritativeModernSpecialistStateStore
    {
        private const int MaximumGauge = 100;
        private const int MaximumSynchronizationLevel = 3;
        private const int DuplicateFallbackMilliseconds = 750;

        private static readonly ConditionalWeakTable<Character, ModernSpecialistState> States =
            new ConditionalWeakTable<Character, ModernSpecialistState>();

        public static void ChangeGauge(BattleEntity entity, int delta, BCardEvent evnt, string source)
        {
            Character character = entity?.Character;
            if (character == null || delta == 0)
            {
                return;
            }

            ModernSpecialistState state = States.GetValue(character, _ => new ModernSpecialistState());
            lock (state.SyncRoot)
            {
                ResetWhenSpecialistChanged(state, character);
                ResetExpiredSynchronization(state, character);

                if (ShouldSuppressRepeatedSkillApplication(state, character, evnt, source))
                {
                    return;
                }

                int beforeGauge = state.Gauge;
                int beforeLevel = state.SynchronizationLevel;

                if (delta > 0 && state.SynchronizationLevel >= MaximumSynchronizationLevel)
                {
                    Logger.Info(
                        $"[SP_SYNC_GAUGE_LOCKED] CharacterId={character.CharacterId} Morph={character.Morph} " +
                        $"Level={state.SynchronizationLevel} Gauge={state.Gauge} Source={source} " +
                        $"SkillVNum={FormatSkill(evnt)} Delta={delta}");
                    return;
                }

                int nextGauge = Math.Max(0, state.Gauge + delta);
                while (nextGauge >= MaximumGauge &&
                       state.SynchronizationLevel < MaximumSynchronizationLevel)
                {
                    nextGauge -= MaximumGauge;
                    state.SynchronizationLevel++;
                    int durationSeconds = GetSynchronizationDurationSeconds(state.SynchronizationLevel);
                    state.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

                    Logger.Info(
                        $"[SP_SYNC_LEVEL_UP] CharacterId={character.CharacterId} Morph={character.Morph} " +
                        $"Level={state.SynchronizationLevel} DurationSeconds={durationSeconds} " +
                        $"Source={source} SkillVNum={FormatSkill(evnt)}");
                }

                if (state.SynchronizationLevel >= MaximumSynchronizationLevel)
                {
                    nextGauge = 0;
                }

                state.Gauge = Math.Min(nextGauge, MaximumGauge - 1);
                state.LastActivityUtc = DateTime.UtcNow;

                Logger.Info(
                    $"[SP_SYNC_GAUGE] CharacterId={character.CharacterId} Morph={character.Morph} " +
                    $"Source={source} SkillVNum={FormatSkill(evnt)} SubType={evnt?.BCard?.SubType ?? 0} " +
                    $"Delta={delta} Gauge={beforeGauge}->{state.Gauge} " +
                    $"Level={beforeLevel}->{state.SynchronizationLevel}");
            }
        }

        public static void ConfigureRule(BattleEntity entity, ModernSynchronizationRule rule, int value, int durationMilliseconds)
        {
            Character character = entity?.Character;
            if (character == null)
            {
                return;
            }

            ModernSpecialistState state = States.GetValue(character, _ => new ModernSpecialistState());
            lock (state.SyncRoot)
            {
                ResetWhenSpecialistChanged(state, character);
                ResetExpiredSynchronization(state, character);
                RemoveExpiredRules(state);

                DateTime expiresAtUtc = durationMilliseconds > 0
                    ? DateTime.UtcNow.AddMilliseconds(durationMilliseconds)
                    : DateTime.MaxValue;

                state.Rules[rule] = new ConfiguredRule
                {
                    Value = Math.Abs(value),
                    ExpiresAtUtc = expiresAtUtc
                };

                Logger.Info(
                    $"[SP_SYNC_RULE_CONFIGURED] CharacterId={character.CharacterId} Morph={character.Morph} " +
                    $"Rule={rule} Value={Math.Abs(value)} DurationMs={durationMilliseconds}");
            }
        }

        public static ModernSpecialistSnapshot GetSnapshot(Character character)
        {
            if (character == null)
            {
                return ModernSpecialistSnapshot.Empty;
            }

            ModernSpecialistState state = States.GetValue(character, _ => new ModernSpecialistState());
            lock (state.SyncRoot)
            {
                ResetWhenSpecialistChanged(state, character);
                ResetExpiredSynchronization(state, character);
                RemoveExpiredRules(state);

                return new ModernSpecialistSnapshot(
                    state.Gauge,
                    state.SynchronizationLevel,
                    state.ExpiresAtUtc,
                    GetRuleValue(state, ModernSynchronizationRule.HeroAttackPercent),
                    GetRuleValue(state, ModernSynchronizationRule.HeroSummonChancePercent),
                    GetRuleValue(state, ModernSynchronizationRule.AttackPercentPerSynchronizationLevel),
                    GetRuleValue(state, ModernSynchronizationRule.DefencePercentPerSynchronizationLevel));
            }
        }

        private static int GetRuleValue(ModernSpecialistState state, ModernSynchronizationRule rule)
        {
            return state.Rules.TryGetValue(rule, out ConfiguredRule configuredRule)
                ? configuredRule.Value
                : 0;
        }

        private static int GetSynchronizationDurationSeconds(int level)
        {
            switch (level)
            {
                case 1:
                    return 60;
                case 2:
                    return 40;
                case 3:
                    return 15;
                default:
                    return 0;
            }
        }

        private static bool ShouldSuppressRepeatedSkillApplication(
            ModernSpecialistState state,
            Character character,
            BCardEvent evnt,
            string source)
        {
            short? skillVNum = evnt?.Skill?.SkillVNum ?? evnt?.BCard?.SkillVNum;
            if (!skillVNum.HasValue)
            {
                return false;
            }

            CharacterSkill characterSkill = character.GetSkills()?
                .FirstOrDefault(skill => skill.SkillVNum == skillVNum.Value);
            long castStamp = evnt?.BCard?.CastType == 0
                ? characterSkill?.LastUse.Ticks ?? 0
                : 0;
            string key = $"{source}:{skillVNum.Value}:{evnt?.BCard?.BCardId ?? 0}";
            DateTime now = DateTime.UtcNow;

            if (state.ProcessedSkillCasts.TryGetValue(key, out ProcessedSkillCast previous))
            {
                if (castStamp != 0 && previous.CastStamp == castStamp)
                {
                    return true;
                }

                if (castStamp == 0 &&
                    (now - previous.ProcessedAtUtc).TotalMilliseconds < DuplicateFallbackMilliseconds)
                {
                    return true;
                }
            }

            state.ProcessedSkillCasts[key] = new ProcessedSkillCast
            {
                CastStamp = castStamp,
                ProcessedAtUtc = now
            };
            return false;
        }

        private static void ResetWhenSpecialistChanged(ModernSpecialistState state, Character character)
        {
            if (state.Morph == character.Morph)
            {
                return;
            }

            state.Morph = character.Morph;
            state.Gauge = 0;
            state.SynchronizationLevel = 0;
            state.ExpiresAtUtc = DateTime.MinValue;
            state.LastActivityUtc = DateTime.UtcNow;
            state.ProcessedSkillCasts.Clear();
            state.Rules.Clear();
        }

        private static void ResetExpiredSynchronization(ModernSpecialistState state, Character character)
        {
            if (state.ExpiresAtUtc == DateTime.MinValue || state.ExpiresAtUtc > DateTime.UtcNow)
            {
                return;
            }

            if (state.Gauge != 0 || state.SynchronizationLevel != 0)
            {
                Logger.Info(
                    $"[SP_SYNC_EXPIRED] CharacterId={character.CharacterId} Morph={character.Morph} " +
                    $"Level={state.SynchronizationLevel} Gauge={state.Gauge}");
            }

            state.Gauge = 0;
            state.SynchronizationLevel = 0;
            state.ExpiresAtUtc = DateTime.MinValue;
            state.ProcessedSkillCasts.Clear();
        }

        private static void RemoveExpiredRules(ModernSpecialistState state)
        {
            DateTime now = DateTime.UtcNow;
            foreach (ModernSynchronizationRule rule in state.Rules
                         .Where(pair => pair.Value.ExpiresAtUtc != DateTime.MaxValue && pair.Value.ExpiresAtUtc <= now)
                         .Select(pair => pair.Key)
                         .ToList())
            {
                state.Rules.Remove(rule);
            }
        }

        private static string FormatSkill(BCardEvent evnt) =>
            evnt?.BCard?.SkillVNum?.ToString() ?? evnt?.Skill?.SkillVNum.ToString() ?? "-";

        private sealed class ProcessedSkillCast
        {
            public long CastStamp { get; set; }

            public DateTime ProcessedAtUtc { get; set; }
        }

        private sealed class ConfiguredRule
        {
            public int Value { get; set; }

            public DateTime ExpiresAtUtc { get; set; }
        }

        private sealed class ModernSpecialistState
        {
            public object SyncRoot { get; } = new object();

            public int Morph { get; set; } = int.MinValue;

            public int Gauge { get; set; }

            public int SynchronizationLevel { get; set; }

            public DateTime LastActivityUtc { get; set; }

            public DateTime ExpiresAtUtc { get; set; }

            public Dictionary<string, ProcessedSkillCast> ProcessedSkillCasts { get; } =
                new Dictionary<string, ProcessedSkillCast>();

            public Dictionary<ModernSynchronizationRule, ConfiguredRule> Rules { get; } =
                new Dictionary<ModernSynchronizationRule, ConfiguredRule>();
        }
    }

    internal enum ModernSynchronizationRule
    {
        HeroAttackPercent,
        HeroSummonChancePercent,
        AttackPercentPerSynchronizationLevel,
        DefencePercentPerSynchronizationLevel
    }

    internal sealed class ModernSpecialistSnapshot
    {
        public static ModernSpecialistSnapshot Empty { get; } =
            new ModernSpecialistSnapshot(0, 0, DateTime.MinValue, 0, 0, 0, 0);

        public ModernSpecialistSnapshot(
            int gauge,
            int synchronizationLevel,
            DateTime expiresAtUtc,
            int heroAttackPercent,
            int heroSummonChancePercent,
            int attackPercentPerSynchronizationLevel,
            int defencePercentPerSynchronizationLevel)
        {
            Gauge = gauge;
            SynchronizationLevel = synchronizationLevel;
            ExpiresAtUtc = expiresAtUtc;
            HeroAttackPercent = heroAttackPercent;
            HeroSummonChancePercent = heroSummonChancePercent;
            AttackPercentPerSynchronizationLevel = attackPercentPerSynchronizationLevel;
            DefencePercentPerSynchronizationLevel = defencePercentPerSynchronizationLevel;
        }

        public int Gauge { get; }

        public int SynchronizationLevel { get; }

        public DateTime ExpiresAtUtc { get; }

        public int HeroAttackPercent { get; }

        public int HeroSummonChancePercent { get; }

        public int AttackPercentPerSynchronizationLevel { get; }

        public int DefencePercentPerSynchronizationLevel { get; }
    }

    public sealed class A01AuthoritativeTokenGaugeHandler : IBCardHandler
    {
        private const byte IncreaseGauge = 41;
        private const byte DecreaseGauge = 42;

        public BCardType.CardType ActionType => BCardType.CardType.TokenGauge;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.BCard == null)
            {
                return;
            }

            switch (evnt.BCard.SubType)
            {
                case IncreaseGauge:
                    AuthoritativeModernSpecialistStateStore.ChangeGauge(
                        evnt.Caster,
                        Math.Abs(evnt.FirstData),
                        evnt,
                        "TokenGauge.Increase");
                    break;
                case DecreaseGauge:
                    AuthoritativeModernSpecialistStateStore.ChangeGauge(
                        evnt.Caster,
                        -Math.Abs(evnt.FirstData),
                        evnt,
                        "TokenGauge.Decrease");
                    break;
                default:
                    ModernSpecialistPendingRules.Log(evnt, "TokenGauge");
                    break;
            }
        }
    }

    public sealed class A02AuthoritativeTokenSpecialistEffectsHandler : IBCardHandler
    {
        private const byte IncreaseGaugeWhenHit = 31;

        public BCardType.CardType ActionType => BCardType.CardType.TokenSpecialistEffects;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.BCard == null || evnt.Target?.Character == null)
            {
                return;
            }

            if (evnt.BCard.SubType != IncreaseGaugeWhenHit)
            {
                ModernSpecialistPendingRules.Log(evnt, "TokenSpecialistEffects");
                return;
            }

            BattleEntity target = evnt.Target;
            int bcardId = evnt.BCard.BCardId;
            int gaugeGain = Math.Abs(evnt.FirstData);
            int maximumTriggers = evnt.BCard.SecondData > 0 ? evnt.BCard.SecondData : 1;
            int triggers = 0;
            DateTime observedLastDefence = target.LastDefence;
            IDisposable subscription = null;

            subscription = Observable.Interval(TimeSpan.FromMilliseconds(100)).Subscribe(_ =>
            {
                if (target.Character == null || target.Character.IsDisposed ||
                    !target.BCardDisposables.ContainsKey(bcardId) ||
                    target.BCardDisposables[bcardId] != subscription)
                {
                    subscription?.Dispose();
                    return;
                }

                DateTime currentLastDefence = target.LastDefence;
                if (currentLastDefence <= observedLastDefence)
                {
                    return;
                }

                observedLastDefence = currentLastDefence;
                triggers++;
                AuthoritativeModernSpecialistStateStore.ChangeGauge(
                    target,
                    gaugeGain,
                    evnt,
                    $"TokenSpecialistEffects.Hit.{triggers}/{maximumTriggers}");

                if (triggers < maximumTriggers)
                {
                    return;
                }

                subscription?.Dispose();
                if (target.BCardDisposables.ContainsKey(bcardId) &&
                    target.BCardDisposables[bcardId] == subscription)
                {
                    target.BCardDisposables.Remove(bcardId);
                }
            });

            target.BCardDisposables[bcardId] = subscription;
            Logger.Info(
                $"[SP_TOKEN_HIT_RULE_ARMED] CharacterId={target.Character.CharacterId} " +
                $"Morph={target.Character.Morph} BCardId={bcardId} Gain={gaugeGain} " +
                $"MaximumTriggers={maximumTriggers}");
        }
    }

    public sealed class A03AuthoritativeDimensionalSynchronizationHandler : IBCardHandler
    {
        private const byte HeroSummonChance = 21;
        private const byte HeroAttack = 31;
        private const byte IncreaseGauge = 41;
        private const byte AttackPerSynchronizationLevel = 51;
        private const byte DefencePerSynchronizationLevel = 52;

        public BCardType.CardType ActionType => BCardType.CardType.DimensionalSynchronization;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.BCard == null)
            {
                return;
            }

            BattleEntity owner = evnt.Target ?? evnt.Caster;
            switch (evnt.BCard.SubType)
            {
                case IncreaseGauge:
                    AuthoritativeModernSpecialistStateStore.ChangeGauge(
                        evnt.Caster,
                        Math.Abs(evnt.FirstData),
                        evnt,
                        "DimensionalSynchronization.Increase");
                    return;
                case HeroSummonChance:
                    AuthoritativeModernSpecialistStateStore.ConfigureRule(
                        owner,
                        ModernSynchronizationRule.HeroSummonChancePercent,
                        evnt.FirstData,
                        evnt.Duration);
                    return;
                case HeroAttack:
                    AuthoritativeModernSpecialistStateStore.ConfigureRule(
                        owner,
                        ModernSynchronizationRule.HeroAttackPercent,
                        evnt.FirstData,
                        evnt.Duration);
                    return;
                case AttackPerSynchronizationLevel:
                    AuthoritativeModernSpecialistStateStore.ConfigureRule(
                        owner,
                        ModernSynchronizationRule.AttackPercentPerSynchronizationLevel,
                        evnt.FirstData,
                        evnt.Duration);
                    return;
                case DefencePerSynchronizationLevel:
                    AuthoritativeModernSpecialistStateStore.ConfigureRule(
                        owner,
                        ModernSynchronizationRule.DefencePercentPerSynchronizationLevel,
                        evnt.FirstData,
                        evnt.Duration);
                    return;
                default:
                    ModernSpecialistPendingRules.Log(evnt, "DimensionalSynchronization");
                    return;
            }
        }
    }
}
