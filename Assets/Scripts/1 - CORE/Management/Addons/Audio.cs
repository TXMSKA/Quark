using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Quark
{
    [Serializable]
    public class Audio : Addon<GameManager>
    {
        #region FIELDS

        [Header("Audio")]
        [SerializeField, Range(0f, 1f)] private float master = 1f;
        [SerializeField, Min(1)] private int capacity = 16;
        [SerializeField] private SFXLibrary library;
        [SerializeField] private AudioMixerGroup sfx;
        [SerializeField] private AudioMixerGroup ui;

        #endregion

        #region LIFETIME

        public override void Hook(GameManager owner)
        {
            base.Hook(owner);
            owner.Values.Set(nameof(Audio), this);
            AudioListener.volume = master;
        }

        public override void Handle()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var voice = active[i];
                if (voice.Source == null) { active.RemoveAt(i); continue; }

                if (voice.Duration > 0f)
                {
                    voice.Elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(voice.Elapsed / voice.Duration);
                    voice.Source.volume = Mathf.Lerp(voice.From, voice.To, t);
                    if (t < 1f) continue;
                    voice.Duration = 0f;
                    if (voice.Releasing) { Recycle(i); continue; }
                }

                if (voice.Source.loop || voice.Source.isPlaying) continue;
                var ended = voice.OnEnded;
                Recycle(i);
                ended?.Invoke();
            }
        }

        public override void Unhook()
        {
            for (var i = active.Count - 1; i >= 0; i--) Recycle(i);
            pool.Clear();
            count = 0;
            if (host != null) UnityEngine.Object.Destroy(host);
            host = null;
            if (Owner != null) Owner.Values.Forget(nameof(Audio));
            base.Unhook();
        }

        #endregion

        #region API

        public float Master
        {
            get => master;
            set { master = Mathf.Clamp01(value); AudioListener.volume = master; }
        }

        // 2D one-shot with no owner: UI, stingers.
        public void Play(string id, float scale = 1f)
        {
            if (library == null || !library.TryGet(id, out var entry)) return;
            var clip = entry.Clip;
            if (clip == null) return;

            var source = Take(null, null);
            if (source == null) return;

            if (ui != null) source.outputAudioMixerGroup = ui;
            source.clip = clip;
            source.volume = Mathf.Clamp01(scale);
            source.pitch = entry.Pitch;
            source.loop = false;
            source.spatialBlend = 0f;
            source.Play();
        }

        public AudioSource Take(Action onEnded, Transform follow)
        {
            var source = Rent();
            if (source == null) return null;

            source.outputAudioMixerGroup = sfx;
            var t = source.transform;
            t.SetParent(follow != null ? follow : Host.transform, false);
            t.localPosition = Vector3.zero;

            active.Add(new Voice { Source = source, OnEnded = onEnded });
            return source;
        }

        public void Release(AudioSource source, float fade = 0f)
        {
            var index = IndexOf(source);
            if (index < 0) return;
            if (fade <= 0f) { Recycle(index); return; }

            var voice = active[index];
            voice.From = source.volume;
            voice.To = 0f;
            voice.Duration = fade;
            voice.Elapsed = 0f;
            voice.Releasing = true;
        }

        public void Fade(AudioSource source, float to, float time)
        {
            var index = IndexOf(source);
            if (index < 0) return;

            var voice = active[index];
            voice.From = source.volume;
            voice.To = to;
            voice.Duration = Mathf.Max(time, 0.0001f);
            voice.Elapsed = 0f;
            voice.Releasing = false;
        }

        #endregion

        #region MISC

        private readonly List<Voice> active = new();
        private readonly Stack<AudioSource> pool = new();
        private GameObject host;
        private int count;

        private GameObject Host
        {
            get
            {
                if (host == null)
                {
                    host = new GameObject("Audio");
                    UnityEngine.Object.DontDestroyOnLoad(host);
                }
                return host;
            }
        }

        private AudioSource Rent()
        {
            if (pool.Count > 0) return pool.Pop();
            if (count >= capacity) return null;

            var go = new GameObject($"Voice {++count}");
            go.transform.SetParent(Host.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private void Recycle(int index)
        {
            var voice = active[index];
            active.RemoveAt(index);

            var source = voice.Source;
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.transform.SetParent(Host.transform, false);
            source.transform.localPosition = Vector3.zero;
            pool.Push(source);
        }

        private int IndexOf(AudioSource source)
        {
            for (var i = 0; i < active.Count; i++)
                if (active[i].Source == source) return i;
            return -1;
        }

        #endregion

        #region CLASSES

        private class Voice
        {
            public AudioSource Source;
            public Action OnEnded;
            public float From, To, Duration, Elapsed;
            public bool Releasing;
        }

        #endregion
    }
}
