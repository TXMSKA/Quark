using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    public abstract class Palette<T> : ScriptableObject where T : UnityEngine.Object
    {
        #region FIELDS

        [SerializeField] private T[] items = Array.Empty<T>();

        #endregion

        #region API

        public T Get(string key) => TryGet(key, out var value) ? value : null;

        public bool TryGet(string key, out T value)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, T>(items.Length, StringComparer.OrdinalIgnoreCase);
                foreach (var item in items)
                    if (item != null) lookup.TryAdd(item.name, item);
            }
            value = null;
            return !string.IsNullOrEmpty(key) && lookup.TryGetValue(key, out value);
        }

        #endregion

        private Dictionary<string, T> lookup;
    }
}
