using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class BulkActivityResult
    {
        public BulkActivityResult(DateTime start)
        {
            Execution = new ExecutionTime(start);
            FailedBatches = new List<FailedBulkSync>();
        }

        public ExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int BatchCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<FailedBulkSync> FailedBatches { get; set; }
    }
}
