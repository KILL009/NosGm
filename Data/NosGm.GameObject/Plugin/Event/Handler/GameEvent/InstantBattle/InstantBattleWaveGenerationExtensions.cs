using System;
using System.Collections.Generic;
using NosGm.GameObject.Event;

namespace NosGm.GameObject.Plugin.Event
{
    internal static class InstantBattleWaveGenerationExtensions
    {
        public static IEnumerable<MonsterToSummon> GenerateMonsters(
            this Map map,
            short monsterVNum,
            int amount,
            bool moving,
            List<EventContainer> deathEvents,
            bool isBonus = false,
            bool isHostile = true,
            bool isBoss = false)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (amount < 0 || amount > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    $"Monster amount must be between 0 and {short.MaxValue}.");
            }

            return map.GenerateMonsters(
                monsterVNum,
                (short)amount,
                moving,
                deathEvents,
                isBonus,
                isHostile,
                isBoss);
        }
    }
}
