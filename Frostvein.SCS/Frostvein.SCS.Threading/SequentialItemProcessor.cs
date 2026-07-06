using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.SCS.Threading
{
    public class SequentialItemProcessor<TItem>
    {
        private readonly object _syncObj = new object();
        private readonly Action<TItem> _processMethod;
        private readonly Queue<TItem> _queue;
        private Task _currentProcessTask;
        private bool _isProcessing;
        private bool _isRunning;

        public SequentialItemProcessor(Action<TItem> processMethod)
        {
            this._processMethod = processMethod;
            this._queue = new Queue<TItem>();
        }

        public void EnqueueMessage(TItem item)
        {
            lock (this._syncObj)
            {
                if (!this._isRunning)
                    return;
                this._queue.Enqueue(item);
                if (this._isProcessing)
                    return;
                this._currentProcessTask = Task.Factory.StartNew(new Action(this.ProcessItem));
            }
        }

        public void Start() => this._isRunning = true;

        public void Stop()
        {
            this._isRunning = false;
            lock (this._syncObj)
                this._queue.Clear();
            if (!this._isProcessing)
                return;
            try
            {
                this._currentProcessTask.Wait();
            }
            catch
            {
            }
        }

        private void ProcessItem()
        {
            TItem obj;
            lock (this._syncObj)
            {
                if (!this._isRunning || this._isProcessing || this._queue.Count <= 0)
                    return;
                this._isProcessing = true;
                obj = this._queue.Dequeue();
            }
            this._processMethod(obj);
            lock (this._syncObj)
            {
                this._isProcessing = false;
                if (!this._isRunning || this._queue.Count <= 0)
                    return;
                this._currentProcessTask = Task.Factory.StartNew(new Action(this.ProcessItem));
            }
        }
    }
}
