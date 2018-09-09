using System;
using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct HubSpotApiDailyRequestCountKeyValue : IKeyValuePair<string, int>
    {
        [BsonField("_id")]
        public string Key => $"{nameof(HubSpotApiDailyRequestCountKeyValue)}_{Date:yyyy-MM-dd}";

        /// <summary>
        /// Limit of 40K/day. Let's hold on to this value for reference.
        /// </summary>
        public int Value { get; set; }

        public DateTime Date { get; set; }

        public DateTime LastUpdated { get; set; }

        public int TimesUpdated { get; set; }
    }
}
