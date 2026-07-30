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

        public event Action<string> OnPlay;
        public event Action OnEnded;

        public void Bind(Transform at) => anchor = at;

        public void Play(string id, Transform at = null, float scale = 1f)
        {
            if (Spawn(id, at, scale, false) != null) OnPlay?.Invoke(id);
        }

        public void Loop(string id, float fade = 0f)
        {
            Stop(fade);
            loop = Spawn(id, null, 1f, true);
            if (loop == null) return;
            if (fade > 0f) { loop.volume = 0f; mixer.Fade(loop, volume, fade); }
            OnPlay?.Invoke(id);
        }

        public void Stop(float fade = 0f)
        {
            if (loop == null) return;
            mixer.Release(loop, fade);
            loop = null;
        }

        #endregion

        #region MISC

        private Transform anchor;
        private AudioSource loop;
        private Audio mixer;

        private AudioSource Spawn(string id, Transform at, float scale, bool looping)
        {
            if (library == null || !library.TryGet(id, out var entry)) return null;
            var clip = entry.Clip;
            if (clip == null) return null;

            if (mixer == null) mixer = GameManager.Find<Audio>();
            if (mixer == null) return null;

            var target = at != null ? at : anchor;
            var source = mixer.Take(looping ? null : () => OnEnded?.Invoke(), target);
            if (source == null) return null;

            source.clip = clip;
            source.volume = volume * scale;
            source.pitch = entry.Pitch;
            source.loop = looping;
            source.spatialBlend = target != null ? spatial : 0f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            return source;
        }

        #endregion
    }
}
