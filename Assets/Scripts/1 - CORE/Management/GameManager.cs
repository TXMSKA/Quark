using System.Collections.Generic;
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
            Boot();
        }

        private void Start()
        {
            services.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            for (var i = 0; i < services.Count; i++) services[i].Boot();
            booted = true;
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

        internal static void Register(Service service)
        {
            if (service is GameManager) return;
            services.Add(service);
            if (Instance != null && Instance.booted) service.Boot();
        }

        internal static void Unregister(Service service) => services.Remove(service);

        #endregion

        #region MISC

        private static readonly List<Service> services = new();
        private bool booted;

        #endregion
    }
}
