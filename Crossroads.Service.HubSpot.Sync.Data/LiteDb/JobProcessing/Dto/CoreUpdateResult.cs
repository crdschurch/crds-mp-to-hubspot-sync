using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class CoreUpdateResult<TUpdateContact> where TUpdateContact : IUpdateContact
    {
        public CoreUpdateResult()
        {
            Failures = new List<CoreUpdateFailure<TUpdateContact>>();
        }

        public CoreUpdateResult(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Failures = new List<CoreUpdateFailure<TUpdateContact>>();
            ContactsThatDoNotExist = new List<EmailAddressCreatedContact>();
            ContactsAlreadyExist = new List<CoreOnlyChangedContact>();
        }

        public IExecutionTime Execution { get; set; }

        public int TotalContacts { get; set; }

        public int SuccessCount { get; set; }

        /// <summary>
        /// (In the context of an update operation) when a contact attempts to update their email
        /// address to one already claimed in HubSpot.
        /// </summary>
        public int ContactAlreadyExistsCount { get; set; }

        public List<CoreOnlyChangedContact> ContactsAlreadyExist { get; set; }

        public int FailureCount { get; set; }

        public List<CoreUpdateFailure<TUpdateContact>> Failures { get; set; }

        public int ContactDoesNotExistCount { get; set; }

        public List<EmailAddressCreatedContact> ContactsThatDoNotExist { get; set; }
    }
}
