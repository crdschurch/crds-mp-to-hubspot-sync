using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class HubSpotApiDailyRequestCountKeyValue : IKeyValuePair<string, int>
    {
        [BsonElement("_id")]
        public string Key => $"{nameof(HubSpotApiDailyRequestCountKeyValue)}_{Date:yyyy-MM-dd}";

        /// <summary>
        /// Limit of 40K/day. Let's hold on to this value for reference.
        /// </summary>
        public int Value { get; set; }

        public DateTime Date { get; set; }

        public DateTime LastUpdatedUtc { get; set; }

        public int TimesUpdated { get; set; }
    }
}
