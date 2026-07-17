using UnityEngine;

namespace Quark
{
    public abstract class Mod<T> : MonoBehaviour where T : Component
    {
        #region API

        public T Owner { get; private set; }

        public virtual void Hook(T owner) => Owner = owner;
        public virtual void Handle() { }
        public virtual void Unhook()
        {
            if (Owner == null) return;
            Owner = null;
        }

        #endregion
    }
}
