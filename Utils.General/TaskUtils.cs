using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Utils.General
{
    internal static class TaskUtils
    {
        public static async void Forget(this Task self, ILogger logger)
        {
            try
            {
                await self;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        public static Task MoveToThreadPool(CancellationToken canceller = default)
        {
            canceller.ThrowIfCancellationRequested();

            var taskSource = new TaskCompletionSource<byte>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    canceller.ThrowIfCancellationRequested();
                    taskSource.SetResult(0);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            });

            return taskSource.Task;
        }

        public static async Task RunUntilCancelledAsync(Func<CancellationToken, Task> f, CancellationToken canceller)
        {
            try
            {
                await MoveToThreadPool(canceller);
                await f(canceller);
            }
            catch (TaskCanceledException)
            {
                // pass
            }
        }

        public static Task DelayMax(Stopwatch stopwatch, TimeSpan timeSpan, CancellationToken canceller = default)
        {
            var n = (timeSpan - stopwatch.Elapsed).Milliseconds;
            var m = Math.Max(n, 0);
            return Task.Delay(TimeSpan.FromMilliseconds(m), canceller);
        }

        public static async Task Timeout(this Task self, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completeTask = await Task.WhenAny(self, timeoutTask);
            if (completeTask != self)
            {
                throw new TimeoutException();
            }
        }

        public static async Task Delay(Func<TimeSpan> delay, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < delay())
            {
                await Task.Delay(interval, cancellationToken);
            }
        }

        public static async Task WaitUntil(Func<bool> condition, TimeSpan checkInterval, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !condition())
            {
                await Task.Delay(checkInterval, cancellationToken);
            }
        }
    }
}