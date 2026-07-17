using UnityEngine;

namespace Quark
{
    public abstract class Entity : Identifiable
    {
        #region FIELDS

        [SerializeField] private Nucleus<Entity> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => nucleus.Initialize(this);
        protected virtual void Update() => nucleus.Handle();
        protected virtual void OnDestroy() => nucleus.Teardown();

        #endregion

        #region API

        public Values Values => nucleus.Values;

        public T Get<T>() where T : class => nucleus.Get<T>();

        #endregion
    }
}
