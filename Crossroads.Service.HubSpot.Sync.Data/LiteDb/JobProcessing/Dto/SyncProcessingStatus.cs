using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct SyncProcessingStatus : IKeyValuePair<string, SyncProcessingState>
    {
        [BsonField("_id")]
        public string Key => nameof(SyncProcessingStatus);

        public SyncProcessingState Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
