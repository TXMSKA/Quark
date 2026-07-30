using UnityEngine;

namespace Quark
{
    public abstract class Service : MonoBehaviour
    {
        #region FIELDS

        [field: SerializeField] public int Priority { get; private set; }
        [SerializeField] private Nucleus<Service> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => GameManager.Register(this);
        protected virtual void Update() => nucleus.Handle();
        protected virtual void OnDestroy()
        {
            GameManager.Unregister(this);
            nucleus.Teardown();
        }

        internal void Boot() => nucleus.Initialize(this);

        #endregion

        #region API

        public Values Values => nucleus.Values;

        public T Get<T>() where T : class => nucleus.Get<T>();

        #endregion
    }
}
