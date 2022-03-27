using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Utils.General
{
    public sealed class UpdateObserver
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly ConcurrentQueue<Action> _actionQueue;
        readonly List<Action> _actionQueueCopy;

        public UpdateObserver()
        {
            _actionQueue = new ConcurrentQueue<Action>();
            _actionQueueCopy = new List<Action>();
        }

        public void Update()
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

        public Task OnUpdate(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskSource = new TaskCompletionSource<byte>();

            _actionQueue.Enqueue(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
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