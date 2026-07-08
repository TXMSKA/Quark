using System;

namespace Quark
{
    [Serializable]
    public abstract class Addon<T>
    {
        public T Owner { get; private set; }

        public virtual void Initialize(T owner) => Owner = owner;
        public virtual void Teardown() => Owner = default;
    }
}
