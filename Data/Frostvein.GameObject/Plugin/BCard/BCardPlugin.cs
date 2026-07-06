using Frostvein.Core;
using Frostvein.Core.Extensions;
using System;
using System.Diagnostics;

namespace Game.Configuration.BCards
{
    public static class BCardPlugin
    {
        public static void Enable()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            long i = 0;

            foreach (var handler in typeof(AddBuffHandler).Assembly.GetTypesImplementingInterface<IBCardHandler>())
            {
                try
                {
                    if (!typeof(IBCardHandler).IsAssignableFrom(handler) || !handler.IsClass)
                    {
                        continue;
                    }

                    var instance = Activator.CreateInstance(handler) as IBCardHandler;
                    PluginFacility.AddBCardHandler(instance, instance.Execute);
                    i++;
                }
                catch (Exception e)
                {
                    Logger.Log.Error($"{handler.FullName} not resolved", e);
                }
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
            Logger.Log.Info($"[ServiceManager]: {elapsedTime} to Load {i} Handler");
        }
    }
}