using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class OperationDatesKeyValue : IKeyValuePair<string, OperationDates>
    {
        [BsonElement("_id")]
        public string Key => nameof(OperationDatesKeyValue);

        public OperationDates Value { get; set; }

        public DateTime LastUpdatedUtc { get; set; }
    }
}
