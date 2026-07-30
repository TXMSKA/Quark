using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public abstract class Addon
    {
        // Toggling mid-action is unsupported: state written in Handle freezes while disabled.
        [field: SerializeField] public bool Enabled { get; set; } = true;

        public abstract void Hook(object owner);
        public virtual void Handle() { }
        public virtual void Unhook() { }
    }

    [Serializable]
    public abstract class Addon<T> : Addon
    {
        [field: NonSerialized] public T Owner { get; private set; }

        public virtual void Hook(T owner) => Owner = owner;
        public override void Hook(object owner) => Hook((T)owner);

        public override void Unhook()
        {
            if (Owner == null) return;
            Owner = default;
        }
    }
}
