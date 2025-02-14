using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal class TaskQueue
    {
        private readonly ConcurrentQueue<Func<Task>> _tasks = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _isProcessing = false;

        public TaskQueue()
        {
            Task.Run(async () => await ProcessQueueAsync(_cancellationTokenSource.Token));
        }

        public void Enqueue(Func<Task> taskGenerator)
        {
            if (taskGenerator == null) throw new ArgumentNullException(nameof(taskGenerator));

            _tasks.Enqueue(taskGenerator);
            if (!_isProcessing)
            {
                _signal.Release();
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(cancellationToken);

                while (_tasks.TryDequeue(out Func<Task> taskGenerator))
                {
                    _isProcessing = true;
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
                _isProcessing = false;
            }
        }
    }
}
