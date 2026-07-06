using Frostvein.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Frostvein.GameObject.Extension
{
    public static class MysteryBoxConfigrationExtension
    {
        private static readonly Random _random = new ((int)(DateTime.UtcNow.Ticks * Environment.TickCount));
        private static readonly MemoryCache _cache = MemoryCache.Default;

        public static readonly List<MysteryBoxReward> Rewards = new()
        {
            new(4700, 1, 5, true),
            new(4325, 1, 4, true),
            new(5932, 1, 25),
            new(5931, 1, 10),
            new(2160, 5, 50),
            new(1118, 1, 50),
            new(1119, 1, 40),
            new(1120, 1, 25),
            new(2173, 5, 50),
            new(1285, 1, 50),
            new(1904, 1, 50),
            new(1296, 2, 50),
            new(5591, 1, 10),
            new(1117, 5, 50),
            new(1286, 2, 50),
            new(1249, 2, 50),
            new(4341, 1, 20),
            new(4342, 1, 10),

        };

        private static int SumChance => GetOrCreateCacheValue(nameof(SumChance), () => Rewards.Where(x => !x.IsLegendary).Sum(x => x.Chance));
        private static int SumChanceLegendary => GetOrCreateCacheValue(nameof(SumChanceLegendary), () => Rewards.Where(x => x.IsLegendary).Sum(x => x.Chance));

        public static void GenerateRewardList(ClientSession Session)
        {
            var packet = GetOrCreateCacheValue(nameof(GenerateRewardList), () =>
            {
                var legendarys = string.Join("\n", Rewards.Where(r => r.IsLegendary).Select(r => $"Legendary: {DAOFactory.ItemDAO.LoadById(r.Vnum).Name}"));
                var trash = string.Join("\n", Rewards.Where(r => !r.IsLegendary).Select(r => DAOFactory.ItemDAO.LoadById(r.Vnum).Name));

                return $"modal 1 {legendarys}\n\n{trash}";
            });
            Session.SendPacket(packet);
        }

        private static T GetOrCreateCacheValue<T>(string key, Func<T> valueFactory)
        {
            var value = _cache.Get(key);

            if (value == null)
            {
                value = valueFactory();
                _cache.Set(key, value, DateTimeOffset.MaxValue);
            }

            return (T)value;
        }

        public static MysteryBoxReward PullReward()
        {
            var randVal = _random.Next(0, 100);
            return (randVal <= 1) ? PullLegendary() : PullTrash();
        }

        private static MysteryBoxReward PullTrash()
        {
            var randVal = _random.Next(SumChance + 1);

            return GetOrCreateCacheValue($"PullTrash_{randVal}", () =>
            {
                foreach (var item in Rewards.Where(x => x.IsLegendary == false))
                {
                    if (item.Chance >= randVal)
                        return item;

                    randVal -= item.Chance;
                }

                throw new IndexOutOfRangeException("Große scheiße Junge");
            });
        }

        private static MysteryBoxReward PullLegendary()
        {
            var randVal = _random.Next(SumChanceLegendary + 1);

            return GetOrCreateCacheValue($"PullLegendary_{randVal}", () =>
            {
                foreach (var item in Rewards.Where(x => x.IsLegendary))
                {
                    if (item.Chance >= randVal)
                        return item;

                    randVal -= item.Chance;
                }

                throw new IndexOutOfRangeException("Große scheiße Junge");
            });
        }
    }

    public readonly struct MysteryBoxReward
    {
        public readonly short Vnum { get; }
        public readonly short Amount { get; }
        public readonly int Chance { get; }
        public readonly bool IsLegendary { get; }

        public MysteryBoxReward(short vnum, short amount, int chance, bool isLegendary = false)
        {
            Vnum = vnum;
            Amount = amount;
            Chance = chance;
            IsLegendary = isLegendary;
        }
    }
}
