
using Frostvein.GameObject.Extension.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Windows.Input;

namespace Frostvein.GameObject.TitanShield.Thread
{
    public static class MemoryCacheThread
    {
        public static async Task Run()
        {
            MemoryCache cache = MemoryCache.Default;

            // Generate a List of all Keys in the Cache
            List<string> cacheKeys = new List<string>(cache.Select(kvp => kvp.Key));
            foreach (string key in cacheKeys)
            {
                object removedItem = cache.Remove(key);
                cache.Trim(100);
            }
            await LoggerService.LogServer.Logger.LogAsync("Memory Cache has been cleared.", Domain.LogType.INFO);
        }
    }
}
