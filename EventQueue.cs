using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CW3_grafika
{
    public class EventQueue
    {
        private readonly Queue<Func<Task>> queue = new Queue<Func<Task>>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task EnqueueAsync(Func<Task> action)
        {
            await semaphore.WaitAsync();

            try
            {
                queue.Enqueue(action);

                if (queue.Count == 1)
                {
                    await ExecuteNextAsync();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExecuteNextAsync()
        {
            if (queue.Count == 0)
            {
                return;
            }

            var action = queue.Peek();
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                // Obsłuż błąd w dowolny sposób
                Console.WriteLine($"Błąd podczas wykonywania zadania: {ex.Message}");
            }
            finally
            {
                queue.Dequeue();
                await ExecuteNextAsync();
            }
        }
    }
}
