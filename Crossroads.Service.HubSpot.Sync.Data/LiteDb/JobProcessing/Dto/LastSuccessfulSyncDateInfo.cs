using System;
using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct LastSuccessfulSyncDateInfo : IKeyValuePair<string, SyncDates>
    {
        [BsonField("_id")]
        public string Key => nameof(LastSuccessfulSyncDateInfo);

        public SyncDates Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
