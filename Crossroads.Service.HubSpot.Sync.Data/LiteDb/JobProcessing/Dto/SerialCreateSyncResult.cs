using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialCreateSyncResult<TContact> where TContact : IContact
    {
        public SerialCreateSyncResult()
        {
            Failures = new List<SerialCreateSyncFailure<TContact>>();
        }

        public SerialCreateSyncResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<SerialCreateSyncFailure<TContact>>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int ContactAlreadyExistsCount { get; set; }

        public int FailureCount { get; set; }

        public List<SerialCreateSyncFailure<TContact>> Failures { get; set; }
    }
}
