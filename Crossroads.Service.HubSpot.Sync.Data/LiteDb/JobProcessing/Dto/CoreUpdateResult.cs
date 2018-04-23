using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class CoreUpdateResult<TUpdateContact> where TUpdateContact : IUpdateContact
    {
        public CoreUpdateResult() { }

        public CoreUpdateResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<CoreUpdateFailure<TUpdateContact>>();
            ContactsThatDoNotExist = new List<EmailAddressCreatedContact>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<CoreUpdateFailure<TUpdateContact>> Failures { get; set; }

        public int ContactDoesNotExistCount { get; set; }

        public List<EmailAddressCreatedContact> ContactsThatDoNotExist { get; set; }
    }
}
