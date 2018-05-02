using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using LiteDB;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncActivity : ISyncActivity
    {
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
    }
}