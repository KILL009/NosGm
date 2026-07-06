using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class PetsRewards
    {
        #region Properties

        [XmlAttribute] public int MateVnum { get; set; }

        #endregion
    }
}