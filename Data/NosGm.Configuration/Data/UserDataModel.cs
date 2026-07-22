using System;
using System.Collections.Generic;
using System.Text;
using NosGm.Domain;

namespace NosGm.Configuration
{
    public class UserDataModel
    {
        public long CharacterId { get; set; }

        public string Name { get; set; }

        public short LevelSum { get; set; }

        public EventType EventType { get; set; }
    }
}
