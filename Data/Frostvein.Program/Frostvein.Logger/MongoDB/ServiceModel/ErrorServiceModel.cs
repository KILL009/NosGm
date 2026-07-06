using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Frostvein.LogServer.MongoDB.LogServiceModel
{
    public class ErrorServiceModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }

        public string Information { get; set; }

        public DateTime DateTime { get; set; }
    }
}
