using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class Lives
    {
        #region Properties

        [XmlAttribute] public byte Value { get; set; }

        #endregion
    }
}