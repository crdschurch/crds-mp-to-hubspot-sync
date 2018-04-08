using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class BulkRunResult
    {
        public BulkRunResult() { }

        public BulkRunResult(DateTime start)
        {
            Execution = new ExecutionTime(start);
            FailedBatches = new List<BulkFailure>();
        }

        public ExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int BatchCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<BulkFailure> FailedBatches { get; set; }
    }
}
