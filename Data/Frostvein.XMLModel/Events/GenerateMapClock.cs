using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Events
{
    [Serializable]
    public class GenerateMapClock
    {
        #region Properties

        [XmlAttribute] public int Value { get; set; }

        #endregion
    }
}