using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using Utils.General;

namespace Utils.Torch
{
    internal static class GameLoopObserver
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MySession), nameof(MySession.Update))]
        static readonly MethodInfo _sessionUpdateMethod;
#pragma warning restore 649

        static readonly ConcurrentQueue<Action> _actionQueue;
        static readonly List<Action> _actionQueueCopy;
        static bool _patched;

        static GameLoopObserver()
        {
            _actionQueue = new ConcurrentQueue<Action>();
            _actionQueueCopy = new List<Action>();
        }

        public static void Release()
        {
            _actionQueue.Clear();
            _actionQueueCopy.Clear();
        }

        public static void Patch(PatchContext ptx)
        {
            var patchMethod = typeof(GameLoopObserver).GetMethod(nameof(OnSessionUpdate), BindingFlags.Static | BindingFlags.NonPublic);
            ptx.GetPattern(_sessionUpdateMethod).Suffixes.Add(patchMethod);
            _patched = true;
        }

        // call in the main loop
        static void OnSessionUpdate()
        {
            _actionQueueCopy.Clear(); // just to be sure

            // prevent infinite loop (when queuing new action inside a queued action)
            while (_actionQueue.TryDequeue(out var action))
            {
                _actionQueueCopy.Add(action);
            }

            foreach (var action in _actionQueueCopy)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            _actionQueueCopy.Clear();
        }

        public static Task MoveToGameLoop(CancellationToken canceller = default)
        {
            if (!_patched)
            {
                throw new Exception("Not patched");
            }

            canceller.ThrowIfCancellationRequested();

            var taskSource = new TaskCompletionSource<byte>();

            _actionQueue.Enqueue(() =>
            {
                try
                {
                    canceller.ThrowIfCancellationRequested();
                    taskSource.TrySetResult(0);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            });

            return taskSource.Task;
        }
    }
}