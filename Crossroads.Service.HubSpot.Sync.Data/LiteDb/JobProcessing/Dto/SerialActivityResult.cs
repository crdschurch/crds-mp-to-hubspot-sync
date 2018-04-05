using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialActivityResult
    {
        public SerialActivityResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<FailedSerialSync>();
        }

        public ExecutionTime Execution { get; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<FailedSerialSync> Failures { get; set; }
    }
}
