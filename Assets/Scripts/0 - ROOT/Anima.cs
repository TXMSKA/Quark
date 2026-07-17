using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    public static class Anima
    {
        #region API

        public static void Alpha(CanvasGroup target, float to, float duration)
        {
            if (target == null) return;
            Instance.Play(new Tween { target = target, from = target.alpha, to = to, duration = Mathf.Max(duration, 0.0001f) });
        }

        #endregion

        #region MISC

        private static Runner runner;
        private static Runner Instance => runner != null ? runner : runner = Spawn();
        private static Runner Spawn() { var host = new GameObject("Anima"); Object.DontDestroyOnLoad(host); return host.AddComponent<Runner>(); }

        private class Tween
        {
            public CanvasGroup target;
            public float from, to, duration, elapsed;

            public bool Step(float delta)
            {
                if (target == null) return true;
                elapsed += delta;
                float t = Mathf.Clamp01(elapsed / duration);
                target.alpha = Mathf.Lerp(from, to, t);
                return t >= 1f;
            }
        }

        private class Runner : MonoBehaviour
        {
            private readonly List<Tween> tweens = new();

            public void Play(Tween tween)
            {
                tweens.RemoveAll(t => t.target == tween.target);
                tweens.Add(tween);
            }

            private void Update()
            {
                for (int i = tweens.Count - 1; i >= 0; i--)
                    if (tweens[i].Step(Time.unscaledDeltaTime)) tweens.RemoveAt(i);
            }
        }

        #endregion
    }
}
