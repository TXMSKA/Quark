using UnityEngine;

namespace Quark
{
    public abstract class Prop : Identifiable
    {
        #region FIELDS
        
        [SerializeField] Nucleus<Prop> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => nucleus.Initialize(this);
        protected virtual void OnDestroy() => nucleus.Teardown();
        
        #endregion
    }
}
