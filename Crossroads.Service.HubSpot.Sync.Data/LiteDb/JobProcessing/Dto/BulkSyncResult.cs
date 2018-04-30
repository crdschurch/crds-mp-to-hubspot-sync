using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class BulkSyncResult
    {
        public BulkSyncResult()
        {
            FailedBatches = new List<BulkSyncFailure>();
        }

        public BulkSyncResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            FailedBatches = new List<BulkSyncFailure>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int BatchCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<BulkSyncFailure> FailedBatches { get; set; }
    }
}
