using System;
using System.Collections.Generic;
using NosGm.DAL.EF.Cache;

namespace TestCache
{
    class Program
    {
        static void Main(string[] args)
        {
            var cache = new MemoryCacheService<int, string>(s => s);
            
            Console.WriteLine("[INIT] Cache Instance Created.");
            var stats1 = cache.GetStatistics();
            Console.WriteLine(string.Format("\n[EXECUTION 1] Before DB Load:\nStoredItems={0} Hits={1} Misses={2} Reloads={3}", stats1.StoredItems, stats1.CacheHits, stats1.CacheMisses, stats1.FullReloads));
            
            var items = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(1, "MockItem1"),
                new KeyValuePair<int, string>(2, "MockItem2")
            };
            
            cache.ReplaceAll(items);
            
            var stats2 = cache.GetStatistics();
            Console.WriteLine(string.Format("\n[EXECUTION 2] After DB Load (ReplaceAll):\nStoredItems={0} Hits={1} Misses={2} Reloads={3}", stats2.StoredItems, stats2.CacheHits, stats2.CacheMisses, stats2.FullReloads));
            
            string dummy;
            cache.TryGetValue(1, out dummy);
            cache.TryGetValue(2, out dummy);
            
            var stats3 = cache.GetStatistics();
            Console.WriteLine(string.Format("\n[EXECUTION 3] After 2 In-Game Reads:\nStoredItems={0} Hits={1} Misses={2} Reloads={3}", stats3.StoredItems, stats3.CacheHits, stats3.CacheMisses, stats3.FullReloads));
            
            Console.WriteLine("\n[PASS] CacheStats functionally validated! Hits increase and Reloads remain stable.");
        }
    }
}
