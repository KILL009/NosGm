using System;
using System.Threading.Tasks;
using System.Threading;

namespace Frostvein.GameObject.TitanShield.Thread
{
    public static class GarbageCollectionThread
    {
        private static Timer garbageCollectionTimer;

        public static void Start()
        {
            garbageCollectionTimer = new Timer(DoGarbageCollection, null, TimeSpan.Zero, TimeSpan.FromMinutes(60));
        }

        private static void DoGarbageCollection(object state)
        {
            long totalMemory = GC.GetTotalMemory(true);
            long startMemory = GC.GetTotalMemory(true);

            GC.Collect();

            long endMemory = GC.GetTotalMemory(true);
            long releasedMemory = startMemory - endMemory;

            // Logge die Ergebnisse
            LoggerService.LogServer.Logger.LogAsync($"[Titan Shield] Total Memory: {startMemory} | Released Memory: {releasedMemory}", Domain.LogType.INFO).Wait();
        }

        public static async Task Run()
        {
            long totalMemory = GC.GetTotalMemory(true);
            long startMemory = GC.GetTotalMemory(true);

            await Task.Run(() => GC.Collect());

            long endMemory = GC.GetTotalMemory(true);
            long releasedMemory = startMemory - endMemory;

            await LoggerService.LogServer.Logger.LogAsync($"[Titan Shield] Total Memory: {startMemory} | Released Memory: {releasedMemory}", Domain.LogType.INFO);
        }
    }
}
