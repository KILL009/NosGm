using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class Gold
    {
        #region Properties

        [XmlAttribute] public long Value { get; set; }

        #endregion
    }
}