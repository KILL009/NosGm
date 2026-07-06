using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class Level
    {
        #region Properties

        [XmlAttribute] public byte Value { get; set; }

        #endregion
    }
}