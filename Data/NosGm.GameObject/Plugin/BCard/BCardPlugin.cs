using NosGm.Core;
using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
    /// Keeps unsupported modern specialist rules observable without flooding the production log.
    /// The authoritative SP10-SP12 handlers live in SESpecialistHandler.cs.
    /// </summary>
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
