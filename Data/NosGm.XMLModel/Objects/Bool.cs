using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class Bool
    {
        #region Properties

        [XmlAttribute] public bool Value { get; set; }

        #endregion
    }
}