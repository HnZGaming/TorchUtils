using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Utils.General
{
    public static class DictionaryPool<K, V>
    {
        static readonly ConcurrentBag<Dictionary<K, V>> _pool = new();

        public static Dictionary<K, V> Create()
        {
            if (_pool.TryTake(out var e)) return e;
            return new Dictionary<K, V>();
        }

        public static void Release(Dictionary<K, V> e)
        {
            e.Clear();
            _pool.Add(e);
        }
    }
}