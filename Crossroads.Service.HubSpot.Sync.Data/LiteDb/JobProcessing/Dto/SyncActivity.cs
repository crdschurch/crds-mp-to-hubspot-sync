using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncActivity : ISyncActivity
    {
        /// <summary>
        /// Contact attribute data is being passed across the wire to HubSpot before the property
        /// meant to receive it has been created in HubSpot.
        /// 
        /// *** ACTION ITEM ***: Create the property in HubSpot.
        /// </summary>
        private const string HubSpotPropertyDoesNotExistSearchString = "PROPERTY_DOESNT_EXIST";

        /// <summary>
        /// Contact attribute data is being passed to a HubSpot contact property that does not
        /// contain the provided value. This is relevant to a HubSpot static list where the allowed
        /// values are predetermined by its current definition.
        /// 
        /// *** ACTION ITEM ***: Create the entry in the HubSpot property's static list.
        /// </summary>
        private const string HubSpotPropertyInvalidOptionSearchString = "INVALID_OPTION";

        public SyncActivity() { }

        public SyncActivity(Guid syncActivityId, DateTime executionStartTime)
        {
            SyncActivityId = syncActivityId;
            Execution = new ExecutionTime(executionStartTime);
            Id = $"{nameof(SyncActivity)}_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
            NewRegistrationOperation = new SyncActivityNewRegistrationOperation();
            CoreUpdateOperation = new SyncActivityCoreUpdateOperation();
        }

        [BsonField("_id")]
        public string Id { get; set; }

        public DateTime LastUpdated { get; set; }

        public Guid SyncActivityId { get; set; }

        public IExecutionTime Execution { get; set; }

        public SyncDates PreviousSyncDates { get; set; }

        public int TotalContacts => NewRegistrationOperation.TotalContacts +
                                    CoreUpdateOperation.TotalContacts;

        public int SuccessCount => NewRegistrationOperation.SuccessCount + CoreUpdateOperation.SuccessCount;

        public int ContactAlreadyExistsCount => NewRegistrationOperation.ContactAlreadyExistsCount +
                                                CoreUpdateOperation.ContactAlreadyExistsCount;

        public int FailureCount => NewRegistrationOperation.FailureCount +
                                   CoreUpdateOperation.RetryFailureCount;

        public int HubSpotApiRequestCount => NewRegistrationOperation.HubSpotApiRequestCount +
                                             CoreUpdateOperation.HubSpotApiRequestCount;

        public ISyncActivityNewRegistrationOperation NewRegistrationOperation { get; set; }

        public ISyncActivityCoreUpdateOperation CoreUpdateOperation { get; set; }

        public SyncProcessingState SyncProcessingState { get; set; }

        public bool HubSpotIssuesWereEncounteredDuringNewRegistrationOperation()
        {
            var failures = NewRegistrationOperation.BulkCreateSyncResult.FailedBatches
                    .Union<IFailureDetails>(NewRegistrationOperation.SerialCreateSyncResult.Failures)
                    .ToList();

            return HubSpotIssuesEncountered(failures);
        }

        public bool HubSpotIssuesWereEncounteredDuringCoreUpdateOperation()
        {
            var failures =
                CoreUpdateOperation.CoreUpdateSyncResult.Failures
                    .Union<IFailureDetails>(CoreUpdateOperation.EmailChangedSyncResult.Failures)
                    .Union(CoreUpdateOperation.RetryCoreUpdateAsCreateSyncResult.Failures)
                    .Union(CoreUpdateOperation.RetryEmailChangeAsCreateSyncResult.Failures)
                .ToList();

            return HubSpotIssuesEncountered(failures);
        }

        /// <summary>
        /// If we've experienced one or more of the following, we'll forestall recording a "last successful
        /// processing date" for a given operation:
        /// 1) received a HTTP status code of 500 from HubSpot.
        /// 2) Property doesn't exist exception.
        /// 3) Property value is not valid exception.
        /// 
        /// HTTP 400s (Bad Request) alone will not stop the recording of a last successful processing date,
        /// as sometimes this is the product of an invalid email address, etc.
        /// </summary>
        /// <param name="failures">Failures recorded while syncing MP contact data to HubSpot.</param>
        private bool HubSpotIssuesEncountered(IReadOnlyCollection<IFailureDetails> failures)
        {
            if (failures == null || failures.Count == 0) return false;

            var httpStatusCodes = new HashSet<HttpStatusCode>(failures.Select(f => f.HttpStatusCode));
            var errors = new HashSet<string>(failures.Select(GetErrors).SelectMany(error => error)); // flatten errors & keep only distinct versions
            return  httpStatusCodes.Contains(HttpStatusCode.InternalServerError) ||  // error on the HubSpot server
                    httpStatusCodes.Contains(HttpStatusCode.Unauthorized) ||         // issue with our HubSpot API key
                    errors.Contains(HubSpotPropertyDoesNotExistSearchString) ||      // HubSpot property doesn't exist (possible user error)
                    errors.Contains(HubSpotPropertyInvalidOptionSearchString);       // Value passed to a static list HubSpot property is invalid (possible user error)
        }

        private IEnumerable<string> GetErrors(IFailureDetails failure)
        {
            return failure?.Exception?.ValidationResults?.Select(result => result?.Error) ?? Enumerable.Empty<string>();
        }
    }
}