using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Frostvein.SCS.Threading
{
    public class Timer
    {
        private readonly System.Threading.Timer _taskTimer;
        private volatile bool _running;
        private volatile bool _performingTasks;

        [CompilerGenerated]
        public event EventHandler Elapsed;

        public int Period { get; set; }

        public bool RunOnStart { get; set; }

        public Timer(int period)
          : this(period, false)
        {
        }

        public Timer(int period, bool runOnStart)
        {
            this.Period = period;
            this.RunOnStart = runOnStart;
            this._taskTimer = new System.Threading.Timer(new TimerCallback(this.TimerCallBack), (object)null, -1, -1);
        }

        public void Start()
        {
            this._running = true;
            this._taskTimer.Change(this.RunOnStart ? 0 : this.Period, -1);
        }

        public void Stop()
        {
            lock (this._taskTimer)
            {
                this._running = false;
                this._taskTimer.Change(-1, -1);
            }
        }

        public void WaitToStop()
        {
            lock (this._taskTimer)
            {
                while (this._performingTasks)
                    Monitor.Wait((object)this._taskTimer);
            }
        }

        private void TimerCallBack(object state)
        {
            lock (this._taskTimer)
            {
                if (!this._running || this._performingTasks)
                    return;
                this._taskTimer.Change(-1, -1);
                this._performingTasks = true;
            }
            try
            {
                if (this.Elapsed == null)
                    return;
                this.Elapsed((object)this, new EventArgs());
            }
            catch
            {
            }
            finally
            {
                lock (this._taskTimer)
                {
                    this._performingTasks = false;
                    if (this._running)
                        this._taskTimer.Change(this.Period, -1);
                    Monitor.Pulse((object)this._taskTimer);
                }
            }
        }
    }
}
