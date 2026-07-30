using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    class CharmStyles : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public int color = -1;
            public string icon;
        }

        public List<Entry> entries = new();
        public SpritePalette icons;
        public Color[] colors =
        {
            new(0.42f, 0.42f, 0.42f),
            new(0.85f, 0.25f, 0.25f),
            new(0.90f, 0.55f, 0.20f),
            new(0.50f, 0.75f, 0.30f),
            new(0.25f, 0.70f, 0.60f),
            new(0.35f, 0.55f, 0.90f),
            new(0.60f, 0.40f, 0.85f),
            new(0.85f, 0.35f, 0.65f),
        };

        static CharmStyles instance;
        public static CharmStyles Instance
        {
            get
            {
                if (instance != null) return instance;
                var guids = AssetDatabase.FindAssets("t:CharmStyles");
                if (guids.Length > 0)
                    return instance = AssetDatabase.LoadAssetAtPath<CharmStyles>(AssetDatabase.GUIDToAssetPath(guids[0]));

                instance = CreateInstance<CharmStyles>();
                if (!AssetDatabase.IsValidFolder("Assets/_/Settings/Editor")) AssetDatabase.CreateFolder("Assets/_/Settings", "Editor");
                AssetDatabase.CreateAsset(instance, "Assets/_/Settings/Editor/CharmStyles.asset");
                AssetDatabase.SaveAssets();
                return instance;
            }
        }

        Dictionary<string, Entry> lookup;

        public Entry Find(string id)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Entry>(entries.Count);
                foreach (var e in entries) lookup.TryAdd(e.id, e);
            }
            return lookup.TryGetValue(id, out var entry) ? entry : null;
        }

        public Entry Write(string id)
        {
            var entry = Find(id);
            if (entry == null)
            {
                entries.Add(entry = new Entry { id = id });
                Invalidate();
            }
            return entry;
        }

        public void Prune(string id)
        {
            var entry = Find(id);
            if (entry == null || entry.color >= 0 || !string.IsNullOrEmpty(entry.icon)) return;
            entries.Remove(entry);
            Invalidate();
        }

        public void Invalidate() => lookup = null;
    }
}
