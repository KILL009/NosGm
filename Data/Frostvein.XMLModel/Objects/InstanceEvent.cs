using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Objects
{
    [Serializable]
    public class InstanceEvent
    {
        #region Properties

        [XmlElement] public CreateMap[] CreateMap { get; set; }

        #endregion
    }
}