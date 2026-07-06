using Frostvein.Domain;
using System;

namespace Frostvein.GameObject
{
    public class Schedule
    {
        #region Properties

        public string DayOfWeek { get; set; }

        public EventType Event { get; set; }

        public int LvlBracket { get; set; }

        public TimeSpan Time { get; set; }

        #endregion
    }
}