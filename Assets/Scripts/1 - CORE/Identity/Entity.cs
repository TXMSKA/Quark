using UnityEngine;

namespace Quark
{
    public abstract class Entity : Identifiable
    {
        #region FIELDS

        [SerializeField] Nucleus<Entity> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => nucleus.Initialize(this);
        protected virtual void OnDestroy() => nucleus.Teardown();
        
        #endregion
    }
}
