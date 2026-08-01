using System.Collections.Generic;

namespace NosGm.AI.Core
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out var value) && value is T casted)
                return casted;
            return default;
        }

        public bool HasKey(string key) => _data.ContainsKey(key);
        
        public void Remove(string key) => _data.Remove(key);
    }
}
