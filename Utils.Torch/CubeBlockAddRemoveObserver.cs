using System;
using NLog;
using Sandbox.Game.Entities;
using Utils.Torch;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;

namespace Utils.Torch
{
    public class CubeBlockAddRemoveObserver<T> : ISceneEntityAddRemoveObserver<T> where T : IMyCubeBlock
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public CubeBlockAddRemoveObserver()
        {
            MyEntities.OnEntityAdd += OnEntityAdd;
            MyEntities.OnEntityRemove += OnEntityRemove;
        }

        public event Action<T> OnAdded;
        public event Action<T> OnRemoved;

        public void Close()
        {
            MyEntities.OnEntityAdd -= OnEntityAdd;
            MyEntities.OnEntityRemove -= OnEntityRemove;
        }

        void OnEntityAdd(MyEntity obj)
        {
            if (obj is MyCubeGrid grid)
            {
                grid.OnBlockAdded += OnGridBlockAdded;
                grid.OnBlockRemoved += OnGridBlockRemoved;

                foreach (IMySlimBlock block in grid.CubeBlocks)
                {
                    OnGridBlockAdded(block);
                }
            }
        }

        void OnEntityRemove(MyEntity obj)
        {
            if (obj is MyCubeGrid grid)
            {
                grid.OnBlockAdded -= OnGridBlockAdded;
                grid.OnBlockRemoved -= OnGridBlockRemoved;

                foreach (IMySlimBlock block in grid.CubeBlocks)
                {
                    OnGridBlockRemoved(block);
                }
            }
        }

        void OnGridBlockAdded(IMySlimBlock block)
        {
            if (block.FatBlock is T fatBlock)
            {
                OnAdded?.Invoke(fatBlock);
            }
        }

        void OnGridBlockRemoved(IMySlimBlock block)
        {
            if (block.FatBlock is T fatBlock)
            {
                OnRemoved?.Invoke(fatBlock);
            }
        }
    }
}