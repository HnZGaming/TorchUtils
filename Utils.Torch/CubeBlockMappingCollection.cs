using System;
using System.Collections.Generic;
using Utils.General;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;

namespace DefaultNamespace
{
    public sealed class CubeBlockMappingCollection<E, T> where E : IMyCubeBlock
    {
        public delegate T Map(E entity);

        readonly ISceneEntityAddRemoveObserver<E> _observer;
        readonly Map _mapper;
        readonly CachingDictionary<long, T> _mappedEntities;
        readonly Throttle<T> _throttle;

        public CubeBlockMappingCollection(ISceneEntityAddRemoveObserver<E> observer, Map mapper)
        {
            _observer = observer;
            _mapper = mapper;

            _observer.OnAdded += OnEntityAdded;
            _observer.OnRemoved += OnEntityRemoved;

            _mappedEntities = new CachingDictionary<long, T>();
            _throttle = new Throttle<T>();
        }

        public void Close()
        {
            _observer.OnAdded -= OnEntityAdded;
            _observer.OnRemoved -= OnEntityRemoved;

            lock (_mappedEntities)
            {
                _mappedEntities.ApplyChanges();
                _mappedEntities.Clear();
            }

            _throttle.Clear();
        }

        void OnEntityAdded(E block)
        {
            var mappedBlock = _mapper(block);
            lock (_mappedEntities)
            {
                _mappedEntities.Add(block.EntityId, mappedBlock);
            }
        }

        void OnEntityRemoved(E block)
        {
            lock (_mappedEntities)
            {
                _mappedEntities.Remove(block.EntityId);
            }
        }

        public IEnumerable<T> Throttle(int count)
        {
            lock (_mappedEntities)
            {
                if (_throttle.Count == 0)
                {
                    _mappedEntities.ApplyChanges();
                    _throttle.Update(_mappedEntities.Values);
                }
            }

            return _throttle.Enumerate(count);
        }

        public IEnumerable<T> GetAll()
        {
            lock (_mappedEntities)
            {
                _mappedEntities.ApplyChanges();
                return _mappedEntities.Values;
            }
        }
    }
}