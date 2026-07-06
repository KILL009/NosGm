using Frostvein.XMLModel.Objects;
using System;
using System.Xml.Serialization;

namespace Frostvein.XMLModel.Events
{
    [Serializable]
    public class OnFirstEnable
    {
        #region Properties

        [XmlElement] public ChangePortalType[] ChangePortalType { get; set; }

        [XmlElement] public NpcDialog[] NpcDialog { get; set; }

        [XmlElement] public OnMapClean OnMapClean { get; set; }

        [XmlElement] public object RefreshMapItems { get; set; }

        [XmlElement] public object RefreshRaidGoals { get; set; }

        [XmlElement] public object RemoveButtonLocker { get; set; }

        [XmlElement] public SendMessage[] SendMessage { get; set; }

        [XmlElement] public object StopMapWaves { get; set; }

        [XmlElement] public SummonMonster[] SummonMonster { get; set; }

        [XmlElement] public Teleport Teleport { get; set; }

        [XmlElement] public SendPacket[] SendPacket { get; set; }

        [XmlElement] public GenerateClock GenerateClock { get; set; }

        [XmlElement] public Wave[] Wave { get; set; }

        #endregion
    }
}