using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class Id
    {
        #region Properties

        [XmlAttribute] public short Value { get; set; }

        #endregion
    }
}