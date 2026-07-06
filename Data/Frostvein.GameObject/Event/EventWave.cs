using System;
using System.Collections.Generic;

namespace Frostvein.GameObject
{
    public class EventWave
    {
        #region Instantiation

        public EventWave(byte delay, List<EventContainer> events, byte offset = 0, short runtimes = short.MaxValue)
        {
            Delay = delay;
            Offset = offset;
            Events = events;
            RunTimes = runtimes;
        }

        #endregion

        #region Properties

        public byte Delay { get; set; }

        public List<EventContainer> Events { get; set; }

        public DateTime LastStart { get; set; }

        public byte Offset { get; set; }

        public short RunTimes { get; set; }

        #endregion
    }
}