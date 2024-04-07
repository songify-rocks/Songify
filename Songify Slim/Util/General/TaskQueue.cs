using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal class TaskQueue
    {
        private readonly ConcurrentQueue<Func<Task>> tasks = new();
        private readonly SemaphoreSlim signal = new(0);
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private bool isProcessing = false;

        public TaskQueue()
        {
            Task.Run(async () => await ProcessQueueAsync(cancellationTokenSource.Token));
        }

        public void Enqueue(Func<Task> taskGenerator)
        {
            if (taskGenerator == null) throw new ArgumentNullException(nameof(taskGenerator));

            tasks.Enqueue(taskGenerator);
            if (!isProcessing)
            {
                signal.Release();
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await signal.WaitAsync(cancellationToken);

                while (tasks.TryDequeue(out Func<Task> taskGenerator))
                {
                    isProcessing = true;
                    try
                    {
                        Task task = taskGenerator.Invoke();
                        await task;
                    }
                    catch
                    {
                        // Log or handle exceptions
                    }
                }
                isProcessing = false;
            }
        }
    }
}
