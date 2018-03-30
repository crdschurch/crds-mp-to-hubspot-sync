﻿using System;
using System.Collections.Generic;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class UpdateContactActivity : IActivity
    {
        public UpdateContactActivity()
        {
            FailedBatches = new List<FailedBatch>();
        }

        [BsonField("_id")]
        public string Id => $"UpdateContactActivity_{ActivityDateTime:u}"; // ISO8601: universal/sortable

        public DateTime ActivityDateTime { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<FailedBatch> FailedBatches { get; set; }
    }
}
