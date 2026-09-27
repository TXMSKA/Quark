using System;
using UnityEngine;

namespace Quark
{
    public abstract class Prop : Identifiable
    {
        #region FIELDS
        
        [SerializeField] private Nucleus<Prop> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => nucleus.Initialize(this);
        protected virtual void Update() => nucleus.Handle();
        protected virtual void OnDestroy() => nucleus.Teardown();

        #endregion

        #region API

        public override Values Values => nucleus.Values;

        public T Get<T>() where T : class => nucleus.Get<T>();

        public event Action<Context> OnUse;
        public event Action<Context> OnInteract;
        public event Action<Context> OnGrab;
        public event Action<Context> OnRelease;

        public virtual void Use(Context context) => OnUse?.Invoke(context);
        public virtual void Interact(Context context) => OnInteract?.Invoke(context);
        public virtual void Grab(Context context) => OnGrab?.Invoke(context);
        public virtual void Release(Context context) => OnRelease?.Invoke(context);

        #endregion
    }
}
