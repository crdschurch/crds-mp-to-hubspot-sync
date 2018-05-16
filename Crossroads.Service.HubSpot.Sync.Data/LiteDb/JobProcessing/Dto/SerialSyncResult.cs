using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialSyncResult
    {
        public SerialSyncResult()
        {
            EmailAddressesAlreadyExist = new List<SerialContact>();
            Failures = new List<SerialSyncFailure>();
        }

        public SerialSyncResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            EmailAddressesAlreadyExist = new List<SerialContact>();
            Failures = new List<SerialSyncFailure>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int InsertCount { get; set; }

        public int UpdateCount { get; set; }

        public int EmailAddressAlreadyExistsCount { get; set; }

        public List<SerialContact> EmailAddressesAlreadyExist { get; set; }

        public int FailureCount { get; set; }

        public List<SerialSyncFailure> Failures { get; set; }
    }
}
