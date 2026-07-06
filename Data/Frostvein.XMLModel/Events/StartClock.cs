using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Events
{
    [Serializable]
    public class StartClock
    {
        #region Properties

        [XmlElement] public OnStop OnStop { get; set; }

        [XmlElement] public OnTimeout OnTimeout { get; set; }

        #endregion
    }
}