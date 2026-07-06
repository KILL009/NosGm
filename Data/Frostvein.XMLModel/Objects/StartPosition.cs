using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class StartPosition
    {
        #region Properties

        [XmlAttribute] public short Value { get; set; }

        #endregion
    }
}