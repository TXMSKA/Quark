using UnityEngine;

namespace Quark
{
    public sealed class Context
    {
        public static readonly Key<Channel> Input = new();
        public static readonly Key<Phase> Stage = new();
        public static readonly Key<Vector3> Point = new();
        public static readonly Key<Ray> View = new();
        public static readonly Key<float> SampleTime = new();
        public static readonly Key<Vector3> Velocity = new();
        public static readonly Key<bool> Stealth = new();

        public Host Source { get; }
        public Host Target { get; }

        public Context(Host source = null, Host target = null)
        {
            Source = source;
            Target = target;
        }

        private Values values;

        public void Set<T>(Key<T> key, T value) => (values ??= new()).Set(key, value);

        public bool TryGet<T>(Key<T> key, out T value)
        {
            value = default;
            return values != null && values.TryGet(key, out value);
        }

        public enum Channel { Primary, Secondary }
        public enum Phase { Press, Release }
    }
}
