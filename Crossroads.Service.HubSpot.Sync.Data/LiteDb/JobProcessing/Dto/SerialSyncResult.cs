using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialSyncResult
    {
        public SerialSyncResult() { }

        public SerialSyncResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<SerialSyncFailure>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int ContactAlreadyExistsCount { get; set; }

        public int FailureCount { get; set; }

        public List<SerialSyncFailure> Failures { get; set; }
    }
}
