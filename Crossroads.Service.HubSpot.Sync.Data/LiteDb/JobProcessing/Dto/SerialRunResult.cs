using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialRunResult
    {
        public SerialRunResult() { }

        public SerialRunResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<SerialFailure>();
        }

        public ExecutionTime Execution { get; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int ContactAlreadyExistsCount { get; set; }

        public int FailureCount { get; set; }

        public List<SerialFailure> Failures { get; set; }
    }
}
