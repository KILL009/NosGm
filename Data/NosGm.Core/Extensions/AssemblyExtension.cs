using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosGm.Core.Extensions
{
    public static class AssemblyExtensions
    {
        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            if (generic == toCheck) return false;

            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) return true;

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static Type[] GetTypesImplementingInterface(this Assembly assembly, params Type[] types)
        {
            var list = new List<Type>();
            foreach (var type in types) list.AddRange(GetTypesImplementingInterface(assembly, new[] { type }));

            return list.ToArray();
        }

        public static Type[] GetTypesImplementingInterface<T>(this Assembly assembly)
        {
            return assembly.GetTypesImplementingInterface(typeof(T));
        }

        public static Type[] GetTypesImplementingInterface(this Assembly assembly, Type type)
        {
            return assembly.GetTypes().Where(s => s.ImplementsInterface(type)).ToArray();
        }

        public static bool ImplementsInterface<T>(this Type type)
        {
            return type.GetInterfaces().Any(s => s == typeof(T));
        }


        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            return type.GetInterfaces().Any(s => s == interfaceType);
        }

        public static Type[] GetTypesImplementingGenericClass(this Assembly assembly, params Type[] types)
        {
            var list = new List<Type>();
            foreach (var type in types) list.AddRange(assembly.GetTypesImplementingGenericClass(type));

            return list.ToArray();
        }

        public static Type[] GetTypesImplementingGenericClass(this Assembly assembly, Type type)
        {
            return assembly.GetTypes().Where(s => IsSubclassOfRawGeneric(type, s)).ToArray();
        }

        public static Type[] GetTypesDerivedFrom<T>(this Assembly assembly)
        {
            return assembly.GetTypes().Where(s => s.IsSubclassOf(typeof(T))).ToArray();
        }
    }
}