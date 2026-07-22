using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class FamMission
    {
        #region Properties

        [XmlAttribute]
        public int Mission1 { get; set; }

        [XmlAttribute]
        public int Mission2 { get; set; }

        #endregion
    }
}
