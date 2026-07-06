using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosTale.ServiceManager.LogService.LogModel
{
    public class ServerInfoLogServiceModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }

        public string Data { get; set; }

        public DateTime DateTime { get; set; }
    }
}
