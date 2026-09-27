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
        [SerializeField] private AudioMixerGroup sfx;

        #endregion

        #region LIFETIME

        public override void Hook(GameManager owner)
        {
            base.Hook(owner);
            owner.Values.Set(Key<Audio>.Default, this);
            AudioListener.volume = master;
        }

        public override void Handle()
        {
            for (var i = active.Count - 1; i >= 0; i--)
                if (active[i] == null || !active[i].isPlaying) Recycle(i);
        }

        public override void Unhook()
        {
            for (var i = active.Count - 1; i >= 0; i--) Recycle(i);
            pool.Clear();
            count = 0;
            if (host != null) UnityEngine.Object.Destroy(host);
            host = null;
            if (Owner != null) Owner.Values.Forget(Key<Audio>.Default);
            base.Unhook();
        }

        #endregion

        #region API

        // The source returns to the pool on the first frame it is not playing.
        public AudioSource Take(Transform follow)
        {
            var source = Rent();
            if (source == null) return null;

            source.outputAudioMixerGroup = sfx;
            var t = source.transform;
            t.SetParent(follow != null ? follow : Host.transform, false);
            t.localPosition = Vector3.zero;

            active.Add(source);
            return source;
        }

        #endregion

        #region MISC

        private readonly List<AudioSource> active = new();
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
            var source = active[index];
            active.RemoveAt(index);
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.transform.SetParent(Host.transform, false);
            source.transform.localPosition = Vector3.zero;
            pool.Push(source);
        }

        #endregion
    }
}
