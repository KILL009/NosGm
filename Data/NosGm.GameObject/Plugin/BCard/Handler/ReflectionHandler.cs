using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NosGm.GameObject._plugins.BCards.Handler
{
    public class ReflectionHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Reflection;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            var firstData = evnt.FirstData;
            var secondData = evnt.BCard.SecondData;
            var subType = evnt.BCard.SubType;

            if (ServerManager.RandomNumber() >= firstData)
            {
                return;
            }

            switch (subType)
            {
                case (byte)AdditionalTypes.Reflection.EnemyMPDecreased:
                    target.DecreaseMp(target.Mp * secondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyMPIncreased:
                    target.IncreaseMp(target.Mp * secondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPDecreased:
                    target.GetDamage(target.Hp * secondData / 100, target);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPIncreased:
                    target.IncreaseHp(target.Hp * secondData / 100);
                    break;
            }

            if (target.Character != null)
            {
                target.Character.Session.SendPacket(target.Character.GenerateStat());
            }
        }
    }

    /// <summary>
    /// Implements the defensive Type 104 family known by modern data as DealDamageAround.
    /// The legacy dispatcher applies these rows while an entity or buff is initialized, so the
    /// handler registers a bounded defensive rule that reacts to structured damage results.
    /// </summary>
    public sealed class DealDamageAroundHandler : IBCardHandler
    {
        private const byte DamageDeflect = 11;
        private const byte SummonOnDefend = 31;
        private const byte SummonOnDefendDouble = 32;

        public BCardType.CardType ActionType => BCardType.CardType.Idk;

        public void Execute(BCardEvent evnt)
        {
            if (evnt?.Target == null || evnt.BCard == null)
            {
                return;
            }

            switch (evnt.BCard.SubType)
            {
                case DamageDeflect:
                case SummonOnDefend:
                case SummonOnDefendDouble:
                    DealDamageAroundRuntime.Register(evnt.Target, evnt.BCard, evnt.FirstData);
                    break;
                default:
                    Game.Configuration.BCards.ModernSpecialistPendingRules.Log(evnt, "DealDamageAround");
                    break;
            }
        }
    }

    internal static class DealDamageAroundRuntime
    {
        private const byte DamageDeflect = 11;
        private const byte SummonOnDefend = 31;
        private const byte SummonOnDefendDouble = 32;
        private const int MaximumRegisteredRules = 4096;
        private const int MaximumOwnedSummonsPerVNum = 4;
        private const int TemporarySummonLifetimeSeconds = 10;

        private static readonly ConcurrentDictionary<string, DefensiveRule> Rules =
            new ConcurrentDictionary<string, DefensiveRule>(StringComparer.Ordinal);

        static DealDamageAroundRuntime()
        {
            CombatDamageDiagnostics.CalculationCompleted += OnDamageCalculated;
        }

        public static void Register(BattleEntity defender, BCard bcard, int effectiveFirstData)
        {
            if (defender == null || bcard == null || bcard.BCardId <= 0)
            {
                return;
            }

            string key = BuildKey(defender, bcard.BCardId);
            IDisposable previous = defender.BCardDisposables?[bcard.BCardId];
            previous?.Dispose();

            if (Rules.Count >= MaximumRegisteredRules && !Rules.ContainsKey(key))
            {
                RemoveStaleRules();
                if (Rules.Count >= MaximumRegisteredRules)
                {
                    Logger.Warn(
                        $"[DEAL_DAMAGE_AROUND_RULE_LIMIT] Limit={MaximumRegisteredRules} " +
                        $"Target={(short)defender.UserType}:{defender.MapEntityId} BCardId={bcard.BCardId}");
                    return;
                }
            }

            Rules[key] = new DefensiveRule(
                key,
                defender,
                bcard.BCardId,
                bcard.SubType,
                effectiveFirstData,
                bcard.SecondData);

            defender.BCardDisposables[bcard.BCardId] = new RuleRegistration(key);
        }

        private static void OnDamageCalculated(DamageCalculationResult result)
        {
            if (result == null || result.FinalDamage <= 0 || result.HitMode == 2 || result.HitMode == 4)
            {
                return;
            }

            foreach (var pair in Rules.ToArray())
            {
                DefensiveRule rule = pair.Value;
                BattleEntity defender = rule?.Target?.Target as BattleEntity;
                if (defender?.MapInstance == null)
                {
                    Rules.TryRemove(pair.Key, out _);
                    continue;
                }

                if (defender.MapEntityId != result.DefenderId || defender.UserType != result.DefenderUserType)
                {
                    continue;
                }

                BattleEntity attacker = defender.MapInstance.BattleEntities.FirstOrDefault(entity =>
                    entity != null &&
                    entity.MapEntityId == result.AttackerId &&
                    entity.UserType == result.AttackerUserType);

                if (attacker == null || attacker == defender || attacker.MapInstance != defender.MapInstance)
                {
                    continue;
                }

                if (!rule.TryMarkHit(result.HitId))
                {
                    continue;
                }

                int chance = ClampPercent(rule.FirstData);
                if (chance <= 0 || chance < 100 && ServerManager.RandomNumber() >= chance)
                {
                    continue;
                }

                switch (rule.SubType)
                {
                    case DamageDeflect:
                        ApplyDamageDeflect(defender, attacker, result.FinalDamage, rule.SecondData);
                        break;
                    case SummonOnDefend:
                        SummonDefenders(defender, attacker, rule.SecondData, 1);
                        break;
                    case SummonOnDefendDouble:
                        SummonDefenders(defender, attacker, rule.SecondData, 2);
                        break;
                }
            }
        }

        private static void ApplyDamageDeflect(BattleEntity defender, BattleEntity attacker, int receivedDamage,
            int rawPercent)
        {
            int percent = ClampPercent(rawPercent);
            long reflectedLong = (long)receivedDamage * percent / 100;
            if (reflectedLong <= 0)
            {
                return;
            }

            int reflectedDamage = reflectedLong >= int.MaxValue ? int.MaxValue : (int)reflectedLong;

            // The legacy MapMonster death pipeline is not centralized in GetDamage. Keep reflected damage
            // non-lethal until that pipeline is unified, preventing zero-HP monsters without death events.
            int appliedDamage = attacker.GetDamage(reflectedDamage, defender, dontKill: true);
            if (appliedDamage <= 0)
            {
                return;
            }

            attacker.MapInstance?.Broadcast(attacker.GenerateDm(appliedDamage));
            attacker.Character?.Session?.SendPacket(attacker.Character.GenerateStat());
        }

        private static void SummonDefenders(BattleEntity defender, BattleEntity attacker, int rawMonsterVNum,
            int requestedAmount)
        {
            if (rawMonsterVNum <= 0 || rawMonsterVNum > short.MaxValue || requestedAmount <= 0)
            {
                return;
            }

            short monsterVNum = (short)rawMonsterVNum;
            if (ServerManager.GetNpcMonster(monsterVNum) == null || defender.MapInstance == null)
            {
                return;
            }

            MapInstance map = defender.MapInstance;
            int alreadyOwned = map.Monsters.Count(monster =>
                monster != null &&
                monster.IsAlive &&
                monster.MonsterVNum == monsterVNum &&
                monster.Owner != null &&
                monster.Owner.MapEntityId == defender.MapEntityId &&
                monster.Owner.UserType == defender.UserType);

            int amount = Math.Min(requestedAmount, MaximumOwnedSummonsPerVNum - alreadyOwned);
            for (int index = 0; index < amount; index++)
            {
                MapCell spawnCell = defender.GetRandomMapCellInRange(2) ?? defender.GetPos();
                var summon = new MonsterToSummon(
                    monsterVNum,
                    spawnCell,
                    attacker,
                    true,
                    isTarget: false,
                    isBonus: false,
                    isHostile: true,
                    isBoss: false,
                    owner: defender,
                    aliveTime: TemporarySummonLifetimeSeconds,
                    aliveTimeMp: 0,
                    noticeRange: 15,
                    hasDelay: 0,
                    maxHp: 0,
                    maxMp: 0);

                int summonedId = map.SummonMonster(summon);
                if (summonedId > 0)
                {
                    ScheduleSummonRemoval(map, summonedId, defender.MapEntityId, defender.UserType);
                }
            }
        }

        private static void ScheduleSummonRemoval(MapInstance map, int summonedId, long ownerId, UserType ownerType)
        {
            Observable.Timer(TimeSpan.FromSeconds(TemporarySummonLifetimeSeconds)).Subscribe(_ =>
            {
                try
                {
                    MapMonster monster = map?.Monsters.Find(candidate => candidate?.MapMonsterId == summonedId);
                    if (monster == null || monster.Owner == null ||
                        monster.Owner.MapEntityId != ownerId || monster.Owner.UserType != ownerType)
                    {
                        return;
                    }

                    map.RemoveMonster(monster);
                    if (monster.BattleEntity != null)
                    {
                        map.Broadcast(monster.BattleEntity.GenerateOut());
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"[DEAL_DAMAGE_AROUND_SUMMON_CLEANUP_FAILED] MonsterId={summonedId}",
                        exception);
                }
            });
        }

        private static void RemoveStaleRules()
        {
            foreach (var pair in Rules.ToArray())
            {
                if (!(pair.Value?.Target?.Target is BattleEntity target) || target.MapInstance == null)
                {
                    Rules.TryRemove(pair.Key, out _);
                }
            }
        }

        private static string BuildKey(BattleEntity target, int bcardId) =>
            $"{RuntimeHelpers.GetHashCode(target)}:{bcardId}";

        private static int ClampPercent(int value)
        {
            long absolute = Math.Abs((long)value);
            return absolute >= 100 ? 100 : (int)absolute;
        }

        private sealed class DefensiveRule
        {
            private readonly object _hitLock = new object();
            private Guid? _lastHitId;

            public DefensiveRule(string key, BattleEntity target, int bcardId, byte subType, int firstData,
                int secondData)
            {
                Key = key;
                Target = new WeakReference(target);
                BCardId = bcardId;
                SubType = subType;
                FirstData = firstData;
                SecondData = secondData;
            }

            public int BCardId { get; }

            public int FirstData { get; }

            public string Key { get; }

            public int SecondData { get; }

            public byte SubType { get; }

            public WeakReference Target { get; }

            public bool TryMarkHit(Guid? hitId)
            {
                if (!hitId.HasValue)
                {
                    return true;
                }

                lock (_hitLock)
                {
                    if (_lastHitId == hitId)
                    {
                        return false;
                    }

                    _lastHitId = hitId;
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
