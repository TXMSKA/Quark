using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    // Map builder data kept with the project: imported palettes, the working palette and brush settings.
    class Shelf : ScriptableObject
    {
        #region FIELDS

        public List<string> palettes = new();
        [Serializable]
        public class Slot
        {
            public List<GameObject> items = new();
        }

        public Slot[] slots = { new(), new(), new(), new(), new() };
        public int slot;
        [SerializeField, HideInInspector] List<GameObject> tray = new();
        public bool snap = true;
        [Min(1f)] public float turnStep = 45f;
        public float radius = 4f;
        [Range(0f, 1f)] public float strength = 0.5f;
        [Range(0f, 90f)] public float slope = 30f;
        public Vector2 scale = new(0.9f, 1.1f);
        public float libraryIcon = 80f;
        public int paletteColumns = 2;

        static Shelf instance;

        #endregion

        #region API

        public static Shelf Instance
        {
            get
            {
                if (instance != null) return instance;
                var guids = AssetDatabase.FindAssets("t:Shelf");
                if (guids.Length > 0)
                    return instance = AssetDatabase.LoadAssetAtPath<Shelf>(AssetDatabase.GUIDToAssetPath(guids[0]));

                instance = CreateInstance<Shelf>();
                if (!AssetDatabase.IsValidFolder("Assets/_/Settings/Editor")) AssetDatabase.CreateFolder("Assets/_/Settings", "Editor");
                AssetDatabase.CreateAsset(instance, "Assets/_/Settings/Editor/Shelf.asset");
                AssetDatabase.SaveAssets();
                return instance;
            }
        }

        // The working palette of the current slot; the old single palette moves into slot 1.
        public List<GameObject> Tray
        {
            get
            {
                if (tray.Count > 0 && slots[0].items.Count == 0) { slots[0].items.AddRange(tray); tray.Clear(); }
                return slots[Mathf.Clamp(slot, 0, slots.Length - 1)].items;
            }
        }

        public void Edit(string label)
        {
            Undo.RecordObject(this, label);
            EditorUtility.SetDirty(this);
        }

        #endregion
    }
}
