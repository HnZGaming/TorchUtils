namespace Utils.General
{
    internal sealed class NullOr<T> where T : class
    {
        readonly T _instance;

        public NullOr()
        {
        }

        public NullOr(T instance)
        {
            _instance = instance;
        }

        public bool TryGet(out T instance)
        {
            instance = _instance;
            return instance != null;
        }
    }

    internal static class NullOr
    {
        public static NullOr<T> NotNull<T>(T instance) where T : class
        {
            return new NullOr<T>(instance);
        }

        public static NullOr<T> Null<T>() where T : class
        {
            return new NullOr<T>();
        }
    }
}