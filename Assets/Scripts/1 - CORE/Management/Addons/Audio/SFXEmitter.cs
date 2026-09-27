using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class SFXEmitter
    {
        #region FIELDS

        [SerializeField] private SFXLibrary library;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0f, 1f)] private float spatial = 1f;
        [SerializeField, Min(0f)] private float minDistance = 5f;
        [SerializeField, Min(0f)] private float maxDistance = 40f;

        #endregion

        #region API

        public void Bind(Transform at) => anchor = at;

        public void Play(string id, Transform at = null, float scale = 1f)
        {
            if (library == null || !library.TryGet(id, out var entry)) return;
            var clip = entry.Clip;
            if (clip == null) return;

            if (mixer == null) mixer = GameManager.Find<Audio>();
            if (mixer == null) return;

            var target = at != null ? at : anchor;
            var source = mixer.Take(target);
            if (source == null) return;

            source.clip = clip;
            source.volume = volume * scale;
            source.pitch = entry.Pitch;
            source.spatialBlend = target != null ? spatial : 0f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
        }

        #endregion

        #region MISC

        private Transform anchor;
        private Audio mixer;

        #endregion
    }
}
