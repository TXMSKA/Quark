namespace Quark
{
    public abstract class Key { }

    public sealed class Key<T> : Key
    {
        public static readonly Key<T> Default = new();
    }
}
