using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Events
{
    [Serializable]
    public class AddClockTime
    {
        #region Properties

        [XmlAttribute] public int Seconds { get; set; }

        #endregion
    }
}