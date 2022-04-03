using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Utils.General
{
    public static class SetPool<T>
    {
        static readonly ConcurrentBag<HashSet<T>> _bag;

        static SetPool()
        {
            _bag = new ConcurrentBag<HashSet<T>>();
        }

        public static HashSet<T> Get()
        {
            if (_bag.TryTake(out var s))
            {
                return s;
            }

            return new HashSet<T>();
        }

        public static void Release(HashSet<T> set)
        {
            set.Clear();
            _bag.Add(set);
        }
    }
}