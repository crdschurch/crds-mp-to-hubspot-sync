using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialSyncResult : ISyncResult
    {
        public SerialSyncResult()
        {
            EmailAddressesAlreadyExist = new List<SerialHubSpotContact>();
            EmailAddressesDoNotExist = new List<SerialHubSpotContact>();
            Failures = new List<SerialSyncFailure>();
        }

        public SerialSyncResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            EmailAddressesAlreadyExist = new List<SerialHubSpotContact>();
            EmailAddressesDoNotExist = new List<SerialHubSpotContact>();
            Failures = new List<SerialSyncFailure>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int InsertCount { get; set; }

        public int UpdateCount { get; set; }

        public int EmailAddressAlreadyExistsCount { get; set; }

        public List<SerialHubSpotContact> EmailAddressesAlreadyExist { get; set; }

        public int FailureCount { get; set; }

        public List<SerialSyncFailure> Failures { get; set; }

        public int EmailAddressDoesNotExistCount { get; set; }

        public List<SerialHubSpotContact> EmailAddressesDoNotExist { get; set; }

        public int DeleteCount { get; set; }

        public int GetCount { get; set; }
    }
}
