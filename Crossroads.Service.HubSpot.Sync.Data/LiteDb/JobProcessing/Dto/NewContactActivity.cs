using System;
using System.Collections.Generic;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class NewContactActivity : IActivity
    {
        public NewContactActivity()
        {
            FailedBatches = new List<FailedBatch>();
        }

        [BsonField("_id")]
        public string Id => $"NewContactActivity_{ActivityDateTime:u}"; // ISO8601: universal/sortable

        public DateTime ActivityDateTime { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<FailedBatch> FailedBatches { get; set; }
    }
}
