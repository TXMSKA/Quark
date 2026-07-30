using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    public class Flow : Service
    {
        #region FIELDS

        [SerializeField, Min(0f)] private float duration = 0.25f;

        #endregion

        #region LIFETIME

        protected override void Awake()
        {
            base.Awake();
            if (GameManager.Instance != null) GameManager.Instance.Values.Set(nameof(Flow), this);
        }

        protected override void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Values.Forget(nameof(Flow));
            base.OnDestroy();
        }

        #endregion

        #region API

        public void Set(CanvasGroup root)
        {
            Close();
            Push(root);
        }

        public void Push(CanvasGroup screen)
        {
            if (screen == null || navigation.Contains(screen)) return;
            navigation.Push(screen);
            Fade(screen, 1f);
        }

        public void Back()
        {
            if (navigation.Count > 1) Fade(navigation.Pop(), 0f);
        }

        public void Close()
        {
            while (navigation.Count > 0) Fade(navigation.Pop(), 0f);
        }

        #endregion

        #region MISC

        private readonly Stack<CanvasGroup> navigation = new();

        private void Fade(CanvasGroup screen, float target)
        {
            if (screen == null) return;
            screen.interactable = screen.blocksRaycasts = target > 0f;
            Anima.Alpha(screen, target, duration);
        }

        #endregion
    }
}
