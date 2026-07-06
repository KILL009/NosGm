using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class FamExp
    {
        #region Properties

        [XmlAttribute] public int Value { get; set; }

        #endregion
    }
}