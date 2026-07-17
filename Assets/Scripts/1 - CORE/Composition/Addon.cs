using System;

namespace Quark
{
    [Serializable]
    public abstract class Addon
    {
        public abstract void Hook(object owner);
        public virtual void Handle() { }
        public virtual void Unhook() { }
    }

    [Serializable]
    public abstract class Addon<T> : Addon
    {
        public T Owner { get; private set; }

        public virtual void Hook(T owner) => Owner = owner;
        public override void Hook(object owner) => Hook((T)owner);

        public override void Unhook()
        {
            if (Owner == null) return;
            Owner = default;
        }
    }
}
