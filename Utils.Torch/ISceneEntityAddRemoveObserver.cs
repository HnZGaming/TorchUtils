using System;
using VRage.Game.ModAPI.Ingame;

namespace Utils.Torch
{
    public interface ISceneEntityAddRemoveObserver<out T> where T : IMyEntity
    {
        event Action<T> OnAdded;
        event Action<T> OnRemoved;
    }
}