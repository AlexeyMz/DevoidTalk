using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DevoidTalk.Core
{
    public sealed class ThreadPool
    {
        readonly ThreadPoolSynchronizationContext syncContext;
        readonly CancellationToken cancellation;

        Thread[] threads;

        readonly BlockingCollection<Action> work = new BlockingCollection<Action>();

        public SynchronizationContext SyncContext
        {
            get { return syncContext; }
        }

        public int CurrentWorkQueueLength
        {
            get { return work.Count; }
        }

        public event EventHandler<Exception> UnhandledException;

        public ThreadPool(int threadCount, CancellationToken cancellation)
        {
            this.syncContext = new ThreadPoolSynchronizationContext(this);
            InitThreads(threadCount);
        }

        private void InitThreads(int threadCount)
        {
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(OnThreadStart)
                {
                    IsBackground = true,
                    Name = $"ThreadPool thread {i}",
                };
                threads[i] = thread;
            }

            foreach (Thread thread in threads)
            {
                thread.Start();
            }
        }

        private void OnThreadStart()
        {
            SynchronizationContext.SetSynchronizationContext(SyncContext);
            
            while (!cancellation.IsCancellationRequested)
            {
                Action action;
                try { action = work.Take(cancellation); }
                catch (OperationCanceledException) { continue; }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Post(() => OnUnhandledException(ex));
                }
            }
        }

        private void OnUnhandledException(Exception ex)
        {
            UnhandledException?.Invoke(this, ex);
        }

        public void Post(Action action)
        {
            work.Add(action);
        }

        public void Post(Func<Task> action) => Post((Action)(() => action()));
    }
}
