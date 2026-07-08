using UnityEngine;

namespace Quark
{
    public abstract class Mod<T> : MonoBehaviour where T : Component
    {
        #region API
        
        public T Owner { get; private set; }

        public virtual void Initialize(T owner) => Owner = owner;
        public virtual void Teardown() => Owner = null;

        #endregion
    }
}
