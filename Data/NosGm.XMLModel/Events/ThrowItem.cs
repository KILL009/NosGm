using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Events
{
    [Serializable]
    public class ThrowItem
    {
        #region Properties

        [XmlAttribute] public short Delay { get; set; }

        [XmlAttribute] public int MaxAmount { get; set; }

        [XmlAttribute] public int MinAmount { get; set; }

        [XmlAttribute] public byte PackAmount { get; set; }

        [XmlAttribute] public short VNum { get; set; }

        #endregion
    }
}