using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal class TaskQueue
    {
        private readonly ConcurrentQueue<Func<Task>> tasks = new ConcurrentQueue<Func<Task>>();
        private readonly SemaphoreSlim signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
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

                while (tasks.TryDequeue(out var taskGenerator))
                {
                    isProcessing = true;
                    try
                    {
                        var task = taskGenerator.Invoke();
                        await task;
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exceptions
                    }
                }
                isProcessing = false;
            }
        }
    }
}
