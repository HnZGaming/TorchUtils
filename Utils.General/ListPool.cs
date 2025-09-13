using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Utils.General
{
    public static class ListPool<T>
    {
        static readonly ConcurrentBag<List<T>> _pool = new();
        public static readonly List<T> Empty = new();

        public static List<T> Get()
        {
            if (_pool.TryTake(out var t))
            {
                return t;
            }

            return new List<T>();
        }

        public static void Release(List<T> t)
        {
            t.Clear();
            _pool.Add(t);
        }
    }
}