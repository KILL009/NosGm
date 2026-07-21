using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reactive.Linq;

namespace Game.Configuration.BCards
{
    public static class BCardPlugin
    {
        public static void Enable()
        {
            var stopWatch = Stopwatch.StartNew();
            Assembly assembly = typeof(IBCardHandler).Assembly;
            Type[] assemblyTypes = GetLoadableTypes(assembly);
            List<Type> handlerTypes = assemblyTypes
                .Where(type => type != null &&
                               type.IsClass &&
                               !type.IsAbstract &&
                               typeof(IBCardHandler).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            int registered = 0;
            int duplicates = 0;
            int failed = 0;

            foreach (Type handlerType in handlerTypes)
            {
                try
                {
                    if (!(Activator.CreateInstance(handlerType) is IBCardHandler instance))
                    {
                        failed++;
                        Logger.Error($"[BCARD_REGISTRY_FAILED] Handler={handlerType.FullName} Reason=ActivatorReturnedNull");
                        continue;
                    }

                    if (PluginFacility.TryAddBCardHandler(instance, instance.Execute, out string existingHandler))
                    {
                        registered++;
                        continue;
                    }

                    duplicates++;
                    Logger.Warn(
                        $"[BCARD_REGISTRY_DUPLICATE] Type={(byte)instance.ActionType} Name={instance.ActionType} " +
                        $"Ignored={handlerType.FullName} Registered={existingHandler ?? "unknown"}");
                }
                catch (Exception exception)
                {
                    failed++;
                    Logger.Error($"[BCARD_REGISTRY_FAILED] Handler={handlerType.FullName}", exception);
                }
            }

            stopWatch.Stop();
            string registeredTypes = string.Join(", ",
                PluginFacility.RegisteredBCardHandlers
                    .OrderBy(pair => (byte)pair.Key)
                    .Select(pair => $"{(byte)pair.Key}:{pair.Key}={pair.Value}"));

            Logger.Info(
                $"[BCARD_REGISTRY] Assembly={assembly.GetName().Name} Location={assembly.Location} " +
                $"Discovered={handlerTypes.Count} Registered={registered} Duplicates={duplicates} " +
                $"Failed={failed} ElapsedMs={stopWatch.ElapsedMilliseconds}");
            Logger.Info($"[BCARD_REGISTRY_TYPES] {registeredTypes}");
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                foreach (Exception loaderException in exception.LoaderExceptions.Where(item => item != null))
                {
                    Logger.Error(
                        $"[BCARD_REGISTRY_TYPELOAD_FAILED] Assembly={assembly.GetName().Name} " +
                        $"Reason={loaderException.GetType().Name}: {loaderException.Message}");
                }

                return exception.Types.Where(type => type != null).ToArray();
            }
        }
    }

    /// <summary>
    /// First authoritative implementation layer for the SP10-SP12 resource system.
    /// State is attached to the Character rather than a transient BattleEntity because
    /// the combat pipeline creates a fresh BattleEntity for individual skill casts.
    /// </summary>
    internal static class ModernSpecialistStateStore
    {
        private const int MaximumGauge = 100;
        private const int MaximumSynchronizationLevel = 3;
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

                int nextGauge = state.Gauge + delta;
                if (nextGauge < 0)
                {
                    nextGauge = 0;
                }

                while (nextGauge >= MaximumGauge &&
                       state.SynchronizationLevel < MaximumSynchronizationLevel)
                {
                    nextGauge -= MaximumGauge;
                    state.SynchronizationLevel++;
                    Logger.Info(
                        $"[SP_SYNC_LEVEL_UP] CharacterId={character.CharacterId} Morph={character.Morph} " +
                        $"Level={state.SynchronizationLevel} Source={source} SkillVNum={FormatSkill(evnt)}");
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

        public static void ConfigureLifetime(BattleEntity entity, BCardEvent evnt)
        {
            Character character = entity?.Character;
            if (character == null || evnt == null || evnt.Duration <= 0)
            {
                return;
            }

            ModernSpecialistState state = States.GetValue(character, _ => new ModernSpecialistState());
            lock (state.SyncRoot)
            {
                ResetWhenSpecialistChanged(state, character);
                DateTime candidate = DateTime.UtcNow.AddMilliseconds(evnt.Duration);
                if (candidate > state.ExpiresAtUtc)
                {
                    state.ExpiresAtUtc = candidate;
                }
            }
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
        }

        private static string FormatSkill(BCardEvent evnt) =>
            evnt?.BCard?.SkillVNum?.ToString() ?? evnt?.Skill?.SkillVNum.ToString() ?? "-";

        private sealed class ModernSpecialistState
        {
            public object SyncRoot { get; } = new object();

            public int Morph { get; set; } = int.MinValue;

            public int Gauge { get; set; }

            public int SynchronizationLevel { get; set; }

            public DateTime LastActivityUtc { get; set; }

            public DateTime ExpiresAtUtc { get; set; }
        }
    }

    /// <summary>
    /// Type 124. Subtype 41 increases the modern specialist gauge and subtype 42
    /// decreases it. The remaining token operations are deliberately reported until
    /// their concrete transport contracts are captured from SP10/SP11.
    /// </summary>
    public sealed class TokenGaugeHandler : IBCardHandler
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
                    ModernSpecialistStateStore.ChangeGauge(
                        evnt.Caster,
                        Math.Abs(evnt.FirstData),
                        evnt,
                        "TokenGauge.Increase");
                    break;

                case DecreaseGauge:
                    ModernSpecialistStateStore.ChangeGauge(
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

    /// <summary>
    /// Type 125. Subtype 31 is used by Achilles' Card 4371: when the owner is hit,
    /// increase the gauge by FirstData, at most SecondData times while the buff lives.
    /// </summary>
    public sealed class TokenSpecialistEffectsHandler : IBCardHandler
    {
        private const byte IncreaseGaugeWhenHit = 31;

        public BCardType.CardType ActionType => BCardType.CardType.TokenSpecialistEffects;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.BCard == null || evnt.Target == null)
            {
                return;
            }

            if (evnt.BCard.SubType != IncreaseGaugeWhenHit)
            {
                ModernSpecialistPendingRules.Log(evnt, "TokenSpecialistEffects");
                return;
            }

            BattleEntity target = evnt.Target;
            if (target.Character == null)
            {
                return;
            }

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
                ModernSpecialistStateStore.ChangeGauge(
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

    /// <summary>
    /// Type 130. The live Achilles data identifies subtype 41 on skill 1961 as the
    /// combat gauge contribution. Card-bound subtypes configure later level/Hero Echo
    /// effects and are retained as observable pending rules instead of being guessed.
    /// </summary>
    public sealed class DimensionalSynchronizationHandler : IBCardHandler
    {
        private const byte IncreaseGauge = 41;

        public BCardType.CardType ActionType => BCardType.CardType.DimensionalSynchronization;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.BCard == null)
            {
                return;
            }

            if (evnt.BCard.SubType == IncreaseGauge)
            {
                ModernSpecialistStateStore.ChangeGauge(
                    evnt.Caster,
                    Math.Abs(evnt.FirstData),
                    evnt,
                    "DimensionalSynchronization.Increase");
                return;
            }

            ModernSpecialistStateStore.ConfigureLifetime(evnt.Target ?? evnt.Caster, evnt);
            ModernSpecialistPendingRules.Log(evnt, "DimensionalSynchronization");
        }
    }

    internal static class ModernSpecialistPendingRules
    {
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private static readonly object SyncRoot = new object();

        public static void Log(BCardEvent evnt, string family)
        {
            if (evnt?.BCard == null)
            {
                return;
            }

            string key = string.Join(":",
                (byte)evnt.BCard.Type,
                evnt.BCard.SubType,
                evnt.BCard.SkillVNum?.ToString() ?? "-",
                evnt.BCard.CardId?.ToString() ?? "-",
                evnt.BCard.BCardId);

            lock (SyncRoot)
            {
                if (!Seen.Add(key))
                {
                    return;
                }
            }

            Logger.Warn(
                $"[SP_MODERN_RULE_PENDING] Family={family} Type={evnt.BCard.Type} " +
                $"SubType={evnt.BCard.SubType} SkillVNum={evnt.BCard.SkillVNum?.ToString() ?? "-"} " +
                $"CardId={evnt.BCard.CardId?.ToString() ?? "-"} BCardId={evnt.BCard.BCardId} " +
                $"FirstData={evnt.FirstData} SecondData={evnt.BCard.SecondData} " +
                $"ThirdData={evnt.BCard.ThirdData} Duration={evnt.Duration}");
        }
    }
}
