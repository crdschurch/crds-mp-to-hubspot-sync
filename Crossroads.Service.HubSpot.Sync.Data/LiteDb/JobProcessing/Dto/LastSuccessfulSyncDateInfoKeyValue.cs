using System;
using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct LastSuccessfulSyncDateInfoKeyValue : IKeyValuePair<string, SyncDates>
    {
        [BsonField("_id")]
        public string Key => nameof(LastSuccessfulSyncDateInfoKeyValue);

        public SyncDates Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
