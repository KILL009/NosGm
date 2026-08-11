using System;
using System.Collections.Generic;
using System.Threading;

namespace NosGm.SCS.Threading
{
    public class SequentialItemProcessor<TItem>
    {
        private readonly ManualResetEventSlim _idle = new ManualResetEventSlim(true);
        private readonly object _syncObj = new object();
        private readonly Action<TItem> _processMethod;
        private readonly Queue<TItem> _queue;
        private readonly int _maximumConcurrency;

        private int _activeWorkers;
        private bool _isRunning;

        public SequentialItemProcessor(Action<TItem> processMethod)
            : this(processMethod, 1)
        {
        }

        public SequentialItemProcessor(
            Action<TItem> processMethod,
            int maximumConcurrency)
        {
            _processMethod = processMethod ??
                throw new ArgumentNullException(nameof(processMethod));
            if (maximumConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumConcurrency),
                    "Maximum concurrency must be positive.");
            }

            _maximumConcurrency = maximumConcurrency;
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

        public int ActiveWorkers
        {
            get
            {
                lock (_syncObj)
                {
                    return _activeWorkers;
                }
            }
        }

        public int MaximumConcurrency => _maximumConcurrency;

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
                _idle.Reset();
                if (_activeWorkers < _maximumConcurrency)
                {
                    _activeWorkers++;
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
                if (_activeWorkers == 0)
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
                TItem item;
                lock (_syncObj)
                {
                    if (!_isRunning || _queue.Count == 0)
                    {
                        _activeWorkers--;
                        if (_activeWorkers == 0)
                        {
                            _idle.Set();
                        }
                        return;
                    }

                    item = _queue.Dequeue();
                }

                try
                {
                    _processMethod(item);
                }
                catch (Exception)
                {
                    // The messenger owns error handling for individual messages.
                }
            }
        }
    }
}
