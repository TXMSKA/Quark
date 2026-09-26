using System;
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

        public override Values Values => nucleus.Values;

        public T Get<T>() where T : class => nucleus.Get<T>();

        public event Action<Context> OnBirth;
        public event Action<Context> OnDeath;

        public virtual void Birth(Context context) => OnBirth?.Invoke(context);
        public virtual void Death(Context context) => OnDeath?.Invoke(context);

        #endregion
    }
}
