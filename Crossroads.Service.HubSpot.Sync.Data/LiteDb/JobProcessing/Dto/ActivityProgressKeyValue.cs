using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct ActivityProgressKeyValue : IKeyValuePair<string, ActivityProgress>
    {
        [BsonField("_id")]
        public string Key => nameof(ActivityProgressKeyValue);

        public ActivityProgress Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
