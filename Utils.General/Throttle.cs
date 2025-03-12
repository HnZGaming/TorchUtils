using System.Collections;
using System.Collections.Generic;

namespace Utils.General
{
    internal class Throttle<T>
    {
        readonly Queue<T> _queue;

        public Throttle()
        {
            _queue = new Queue<T>();
        }

        public int Count => _queue.Count;

        public void Clear()
        {
            _queue.Clear();
        }

        public void Update(IEnumerable<T> src)
        {
            if (_queue.Count == 0)
            {
                _queue.EnqueueAll(src);
            }
        }

        public IEnumerable<T> Enumerate(int count)
        {
            return new Enumerator(this, count);
        }

        struct Enumerator : IEnumerator<T>, IEnumerable<T>
        {
            readonly Throttle<T> _self;
            readonly int _maxMaxCount;
            int _count;
            T _current;

            public Enumerator(Throttle<T> self, int maxCount)
            {
                _self = self;
                _maxMaxCount = maxCount;
                _count = 0;
                _current = default;
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public IEnumerator<T> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_count >= _maxMaxCount) return false;
                if (!_self._queue.TryDequeue(out _current)) return false;
                _count += 1;
                return true;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                _current = default;
            }
        }
    }
}