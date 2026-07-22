using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class Id
    {
        #region Properties

        [XmlAttribute] public short Value { get; set; }

        #endregion
    }
}