using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosTale.ServiceManager.LogService.LogModel
{
    public class ServerErrorLogServiceModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }

        public string Information { get; set; }

        public DateTime DateTime { get; set; }
    }
}
