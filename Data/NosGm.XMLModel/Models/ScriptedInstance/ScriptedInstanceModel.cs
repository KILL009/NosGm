using NosGm.XMLModel.Objects;
using System;
using System.Xml.Serialization;

namespace NosGm.XMLModel.Models.ScriptedInstance
{
    [XmlRoot("Definition"), Serializable]
    public class ScriptedInstanceModel
    {
        #region Properties

        public Globals Globals { get; set; }

        public InstanceEvent InstanceEvents { get; set; }

        #endregion
    }
}