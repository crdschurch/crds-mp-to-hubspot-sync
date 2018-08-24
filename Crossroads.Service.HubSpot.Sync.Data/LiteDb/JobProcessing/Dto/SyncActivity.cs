using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using LiteDB;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncActivity : ISyncActivity
    {
        public SyncActivity()
        {
            NewRegistrationOperation = new SyncActivityOperation();
            CoreUpdateOperation = new SyncActivityOperation();
            ChildAgeAndGradeUpdateOperation = new SyncActivityChildAgeAndGradeUpdateOperation();
        }

        public SyncActivity(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Id = $"{nameof(SyncActivity)}_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
            NewRegistrationOperation = new SyncActivityOperation();
            CoreUpdateOperation = new SyncActivityOperation();
            ChildAgeAndGradeUpdateOperation = new SyncActivityChildAgeAndGradeUpdateOperation();
        }

        [BsonField("_id")]
        public string Id { get; set; }

        public DateTime LastUpdated { get; set; }

        public IExecutionTime Execution { get; set; }

        public SyncDates PreviousSyncDates { get; set; }

        public int TotalContacts => NewRegistrationOperation.TotalContacts +
                                    CoreUpdateOperation.TotalContacts +
                                    ChildAgeAndGradeUpdateOperation.TotalContacts;

        public int SuccessCount => NewRegistrationOperation.SuccessCount +
                                   CoreUpdateOperation.SuccessCount +
                                   ChildAgeAndGradeUpdateOperation.SuccessCount;

        public int EmailAddressAlreadyExistsCount => NewRegistrationOperation.EmailAddressAlreadyExistsCount +
                                                     CoreUpdateOperation.EmailAddressAlreadyExistsCount +
                                                     ChildAgeAndGradeUpdateOperation.EmailAddressAlreadyExistsCount;

        public int FailureCount => NewRegistrationOperation.FailureCount +
                                   CoreUpdateOperation.FailureCount +
                                   ChildAgeAndGradeUpdateOperation.RetryFailureCount;

        public int HubSpotApiRequestCount => NewRegistrationOperation.HubSpotApiRequestCount +
                                             CoreUpdateOperation.HubSpotApiRequestCount +
                                             ChildAgeAndGradeUpdateOperation.HubSpotApiRequestCount;

        public ISyncActivityOperation NewRegistrationOperation { get; set; }

        public ISyncActivityOperation CoreUpdateOperation { get; set; }

        public ISyncActivityChildAgeAndGradeUpdateOperation ChildAgeAndGradeUpdateOperation { get; set; }

        public SyncProcessingState SyncProcessingState { get; set; }
    }
}