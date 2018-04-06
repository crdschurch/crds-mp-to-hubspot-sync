﻿using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class NewContactActivityResult : IActivityResult
    {
        public NewContactActivityResult()
        {
        }

        public NewContactActivityResult(DateTime executionStartTime)
        {
            BulkRunResults = new List<BulkActivityResult>();
            Execution = new ExecutionTime(executionStartTime);
            Id = $"NewContactActivityResult_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
        }

        [BsonField("_id")]
        public string Id { get; set; }

        public DateTime LastUpdated { get; set; }

        public ExecutionTime Execution { get; set; }

        public DateTime LastSuccessfulSyncDate { get; set; }

        public int TotalContacts { get; set; }

        public List<BulkActivityResult> BulkRunResults { get; set; }

        public SerialActivityResult SerialRunResult { get; set; }

        public JobProcessingState JobProcessingState { get; set; }
    }
}
