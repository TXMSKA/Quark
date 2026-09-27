using System.Collections.Generic;

namespace Quark
{
    public sealed class Values
    {
        private class Cell<T> { public T Value; }

        private readonly Dictionary<Key, object> table = new();

        public void Set<T>(Key<T> key, T value)
        {
            if (table.TryGetValue(key, out var entry) && entry is Cell<T> cell) cell.Value = value;
            else table[key] = new Cell<T> { Value = value };
        }

        public bool TryGet<T>(Key<T> key, out T value)
        {
            if (table.TryGetValue(key, out var entry) && entry is Cell<T> cell)
            {
                value = cell.Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool Forget<T>(Key<T> key) => table.Remove(key);
    }
}
