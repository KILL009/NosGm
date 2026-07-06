using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace Frostvein.GameObject
{
    public class EventSchedule : IConfigurationSectionHandler
    {
        #region Methods

        public object Create(object parent, object configContext, XmlNode section)
        {
            var list = new List<Schedule>();
            foreach (XmlNode aSchedule in section.ChildNodes) list.Add(getSchedule(aSchedule));
            return list;
        }

        private static Schedule getSchedule(XmlNode str)
        {
            if (str.Attributes != null)
            {
                var lvlBracket = 0;
                if (str.Attributes["lvlbracket"] != null) lvlBracket = int.Parse(str.Attributes["lvlbracket"].Value);
                var dayOfWeek = "";
                if (str.Attributes["dayOfWeek"] != null) dayOfWeek = str.Attributes["dayOfWeek"].Value;
                return new Schedule
                {
                    Event = (EventType)Enum.Parse(typeof(EventType), str.Attributes["event"].Value),
                    Time = TimeSpan.Parse(str.Attributes["time"].Value),
                    LvlBracket = lvlBracket,
                    DayOfWeek = dayOfWeek
                };
            }

            return null;
        }

        #endregion
    }
}