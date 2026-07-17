using System.Collections.Generic;

namespace Quark
{
    public sealed class Values
    {
        private class Cell<T> { public T Value; }

        private readonly Dictionary<string, object> table = new();

        public void Set<T>(string key, T value)
        {
            if (table.TryGetValue(key, out var entry) && entry is Cell<T> cell) cell.Value = value;
            else table[key] = new Cell<T> { Value = value };
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (table.TryGetValue(key, out var entry) && entry is Cell<T> cell)
            {
                value = cell.Value;
                return true;
            }
            value = default;
            return false;
        }

        public T Get<T>(string key) => TryGet(key, out T value) ? value : throw new KeyNotFoundException(key);

        public bool Has(string key) => table.ContainsKey(key);
        public bool Forget(string key) => table.Remove(key);
    }
}
