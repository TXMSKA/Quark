using System.Collections.Generic;
using System.Linq;
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

        // Addons register under their default key; Services are found by type, without registering.
        public static T Find<T>() where T : class =>
            Instance != null && Instance.Values.TryGet(Key<T>.Default, out T value)
                ? value
                : services.OfType<T>().FirstOrDefault();

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
