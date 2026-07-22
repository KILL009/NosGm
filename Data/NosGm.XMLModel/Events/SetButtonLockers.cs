using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Events
{
    [Serializable]
    public class SetButtonLockers
    {
        #region Properties

        [XmlAttribute] public byte Value { get; set; }

        #endregion
    }
}