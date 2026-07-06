using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Events
{
    [Serializable]
    public class OnEnable
    {
        #region Properties

        [XmlElement] public NpcDialog[] NpcDialog { get; set; }

        [XmlElement] public Teleport Teleport { get; set; }

        #endregion
    }
}