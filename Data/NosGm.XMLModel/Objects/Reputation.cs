using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class Reputation
    {
        #region Properties

        [XmlAttribute] public int Value { get; set; }

        #endregion
    }
}