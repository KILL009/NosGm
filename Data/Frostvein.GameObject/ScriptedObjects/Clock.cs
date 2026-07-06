using Frostvein.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Frostvein.GameObject
{
    public class Clock
    {
        #region Instantiation

        public Clock(byte type)
        {
            StopEvents = new List<EventContainer>();
            TimeoutEvents = new List<EventContainer>();
            Type = type;
            SecondsRemaining = 1;
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x => tick());
        }

        #endregion

        #region Properties

        public bool Enabled { get; private set; }

        public int SecondsRemaining { get; set; }

        public List<EventContainer> StopEvents { get; set; }

        public List<EventContainer> TimeoutEvents { get; set; }

        public int TotalSecondsAmount { get; set; }

        public byte Type { get; set; }

        #endregion

        #region Methods

        public void AddTime(int seconds)
        {
            SecondsRemaining += seconds * 10;
            TotalSecondsAmount += seconds * 10;
        }

        public string GetClock() => $"evnt {Type} {(Enabled ? 0 : Type != 3 ? -1 : 1)} {SecondsRemaining} {TotalSecondsAmount}";

        public void StartClock()
        {
            Enabled = true;
        }

        public void StopClock()
        {
            Enabled = false;
            StopEvents.ForEach(e => EventHelper.Instance.RunEvent(e));
            StopEvents.RemoveAll(s => s != null);
        }

        private void tick()
        {
            if (Enabled)
            {
                if (SecondsRemaining > 0)
                {
                    SecondsRemaining -= 10;
                }
                else
                {
                    TimeoutEvents.ForEach(ev => EventHelper.Instance.RunEvent(ev));
                    TimeoutEvents.RemoveAll(s => s != null);
                }
            }
        }

        #endregion
    }
}