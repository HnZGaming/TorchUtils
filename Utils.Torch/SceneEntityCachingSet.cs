using System.Collections;
using System.Collections.Generic;
using NLog;
using Utils.Torch;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;

namespace HNZ.Utils
{
    public sealed class SceneEntityCachingSet<T> : IEnumerable<T> where T : IMyEntity
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly ISceneEntityAddRemoveObserver<T> _observer;
        readonly ConcurrentCachingHashSet<T> _entities;

        public SceneEntityCachingSet(ISceneEntityAddRemoveObserver<T> observer)
        {
            _observer = observer;
            _entities = new ConcurrentCachingHashSet<T>();
            observer.OnAdded += OnEntityAdd;
            observer.OnRemoved += OnEntityRemove;
        }

        public void Clear()
        {
            _observer.OnAdded -= OnEntityAdd;
            _observer.OnRemoved -= OnEntityRemove;
        }

        void OnEntityAdd(T obj)
        {
            _entities.Add(obj);
        }

        void OnEntityRemove(T obj)
        {
            _entities.Remove(obj);
        }

        public void ApplyChanges()
        {
            _entities.ApplyChanges();
        }

        public IEnumerator<T> GetEnumerator() => _entities.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}