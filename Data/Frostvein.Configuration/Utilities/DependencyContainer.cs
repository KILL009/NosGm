using System;
using System.Collections.Generic;

namespace Frostvein.Configuration.Utilities
{
    public class DependencyContainer
    {
        private static readonly Lazy<DependencyContainer> Lazy = new Lazy<DependencyContainer>(() => new DependencyContainer());

        public static DependencyContainer Instance => Lazy.Value;
        private readonly Dictionary<Type, object> _objects = new Dictionary<Type, object>();

        public void Register<T>(T instance) where T : class
        {
            _objects[typeof(T)] = instance;
        }

        public T GetInstance<T>() where T : class => !_objects.TryGetValue(typeof(T), out object instance) ? null : instance as T;
    }
}