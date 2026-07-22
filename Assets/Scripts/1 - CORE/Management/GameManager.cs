using UnityEngine;

namespace Quark
{
    [DefaultExecutionOrder(-100)]
    public abstract class GameManager : Service
    {
        #region LIFETIME

        protected override void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #endregion

        #region API

        public static GameManager Instance { get; private set; }

        public static T Find<T>(string key = null) where T : class =>
            Instance != null && Instance.Values.TryGet(key ?? typeof(T).Name, out T value) ? value : null;

        #endregion
    }
}
