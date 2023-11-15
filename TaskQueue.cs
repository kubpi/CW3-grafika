using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CW3_grafika
{
    public class TaskQueue
    {
        private readonly Queue<Action> tasks = new Queue<Action>();
        private bool isProcessing = false;

        public void Enqueue(Action task)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
                if (!isProcessing)
                {
                    isProcessing = true;
                    Task.Run(() => ProcessQueue());
                }
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                Action task;
                lock (tasks)
                {
                    if (tasks.Count == 0)
                    {
                        isProcessing = false;
                        return;
                    }
                    task = tasks.Dequeue();
                }
                task?.Invoke();
            }
        }
    }

}
