using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    [CreateAssetMenu(menuName = "Quark/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        #region FIELDS

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        #endregion

        #region LIFETIME

        private void OnValidate()
        {
            foreach (var e in entries)
                if (e != null && e.pitch == Vector2.zero) e.pitch = Vector2.one;
        }

        #endregion

        #region API

        public bool TryGet(string id, out Entry entry)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Entry>(entries.Length, StringComparer.OrdinalIgnoreCase);
                foreach (var e in entries)
                    if (e != null && !string.IsNullOrEmpty(e.Id)) lookup.TryAdd(e.Id, e);
            }
            entry = null;
            return !string.IsNullOrEmpty(id) && lookup.TryGetValue(id, out entry);
        }

        #endregion

        #region MISC

        private Dictionary<string, Entry> lookup;

        #endregion

        [Serializable]
        public class Entry
        {
            #region FIELDS

            [field: SerializeField] public string Id { get; private set; }
            [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
            [SerializeField, MinMax(0f, 2f)] internal Vector2 pitch = Vector2.one;

            #endregion

            #region API

            public AudioClip Clip => clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
            public float Pitch => UnityEngine.Random.Range(pitch.x, pitch.y);

            #endregion
        }
    }
}
