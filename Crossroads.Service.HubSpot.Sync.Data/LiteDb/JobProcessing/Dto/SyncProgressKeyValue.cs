using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct SyncProgressKeyValue : IKeyValuePair<string, SyncProgress>
    {
        [BsonField("_id")]
        public string Key => nameof(SyncProgressKeyValue);

        public SyncProgress Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
