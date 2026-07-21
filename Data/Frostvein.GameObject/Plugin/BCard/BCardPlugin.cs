using Frostvein.Core;
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
}
