using System;
using Crossroads.Service.HubSpot.Sync.LiteDb;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct LastSuccessfulOperationDateInfoKeyValue : IKeyValuePair<string, OperationDates>
    {
        [BsonField("_id")]
        public string Key => nameof(LastSuccessfulOperationDateInfoKeyValue);

        public OperationDates Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
