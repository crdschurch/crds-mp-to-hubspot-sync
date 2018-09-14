using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class ActivityProgressKeyValue : IKeyValuePair<string, ActivityProgress>
    {
        [BsonElement("_id")]
        public string Key => nameof(ActivityProgressKeyValue);

        public ActivityProgress Value { get; set; }

        public DateTime LastUpdatedUtc { get; set; }
    }
}
