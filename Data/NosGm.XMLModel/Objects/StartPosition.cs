using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class StartPosition
    {
        #region Properties

        [XmlAttribute] public short Value { get; set; }

        #endregion
    }
}