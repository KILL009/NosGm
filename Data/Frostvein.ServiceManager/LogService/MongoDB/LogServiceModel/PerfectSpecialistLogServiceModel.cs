using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosTale.ServiceManager.LogService.LogModel
{
    public class PerfectSpepcialistLogServiceModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }
        public long CharacterId { get; set; }

        public string Name { get; set; }

        public string Information { get; set; }

        public string Result { get; set; }

        public string ItemGUID { get; set; }

        public DateTime DateTime { get; set; }
    }
}
