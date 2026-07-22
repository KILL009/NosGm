using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Objects
{
    [Serializable]
    public class InstanceEvent
    {
        #region Properties

        [XmlElement] public CreateMap[] CreateMap { get; set; }

        #endregion
    }
}