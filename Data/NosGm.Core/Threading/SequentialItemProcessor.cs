using System;
using System.Collections.Generic;
using System.Threading;

namespace NosGm.Core.Threading
{
    /// <summary>
    /// Processes queued items sequentially while allowing producers to enqueue
    /// from multiple threads. A single ThreadPool worker drains the queue instead
    /// of scheduling one work item for every message.
    /// </summary>
    /// <typeparam name="TItem">Type of item to process.</typeparam>
    public class SequentialItemProcessor<TItem>
    {
        private readonly ManualResetEventSlim _idle = new ManualResetEventSlim(true);
        private readonly Action<TItem> _processMethod;
        private readonly Queue<TItem> _queue;
        private readonly object _syncObj = new object();

        private bool _isProcessing;
        private bool _isRunning;

        public SequentialItemProcessor(Action<TItem> processMethod)
        {
            _processMethod = processMethod ?? throw new ArgumentNullException(nameof(processMethod));
            _queue = new Queue<TItem>();
        }

        public int QueueDepth
        {
            get
            {
                lock (_syncObj)
                {
                    return _queue.Count;
                }
            }
        }

        public void ClearQueue()
        {
            lock (_syncObj)
            {
                _queue.Clear();
            }
        }

        public void EnqueueMessage(TItem item)
        {
            bool startWorker = false;
            lock (_syncObj)
            {
                if (!_isRunning)
                {
                    return;
                }

                _queue.Enqueue(item);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _idle.Reset();
                    startWorker = true;
                }
            }

            if (startWorker)
            {
                ThreadPool.QueueUserWorkItem(ProcessItems);
            }
        }

        public void Start()
        {
            lock (_syncObj)
            {
                _isRunning = true;
            }
        }

        public void Stop()
        {
            lock (_syncObj)
            {
                _isRunning = false;
                _queue.Clear();
                if (!_isProcessing)
                {
                    _idle.Set();
                }
            }

            _idle.Wait();
        }

        private void ProcessItems(object state)
        {
            while (true)
            {
                TItem itemToProcess;
                lock (_syncObj)
                {
                    if (!_isRunning || _queue.Count == 0)
                    {
                        _isProcessing = false;
                        _idle.Set();
                        return;
                    }

                    itemToProcess = _queue.Dequeue();
                }

                try
                {
                    _processMethod(itemToProcess);
                }
                catch (Exception)
                {
                    // Message dispatch failures are handled by the owning messenger.
                }
            }
        }
    }
}
