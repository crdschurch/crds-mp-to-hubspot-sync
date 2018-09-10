using LiteDB;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class Activity : IActivity
    {
        public Activity()
        {
            NewRegistrationSyncOperation = new ActivitySyncOperation();
            CoreContactAttributeSyncOperation = new ActivitySyncOperation();
            ChildAgeAndGradeSyncOperation = new ActivityChildAgeAndGradeSyncOperation();
        }

        public Activity(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Id = $"{nameof(Activity)}_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
            NewRegistrationSyncOperation = new ActivitySyncOperation();
            CoreContactAttributeSyncOperation = new ActivitySyncOperation();
            ChildAgeAndGradeSyncOperation = new ActivityChildAgeAndGradeSyncOperation();
        }

        [BsonField("_id")]
        public string Id { get; set; }

        public DateTime LastUpdated { get; set; }

        public IExecutionTime Execution { get; set; }

        public OperationDates PreviousOperationDates { get; set; }

        public int TotalContacts => NewRegistrationSyncOperation.TotalContacts +
                                    CoreContactAttributeSyncOperation.TotalContacts +
                                    ChildAgeAndGradeSyncOperation.TotalContacts;

        public int SuccessCount => NewRegistrationSyncOperation.SuccessCount +
                                   CoreContactAttributeSyncOperation.SuccessCount +
                                   ChildAgeAndGradeSyncOperation.SuccessCount;

        public int EmailAddressAlreadyExistsCount => NewRegistrationSyncOperation.EmailAddressAlreadyExistsCount +
                                                     CoreContactAttributeSyncOperation.EmailAddressAlreadyExistsCount +
                                                     ChildAgeAndGradeSyncOperation.EmailAddressAlreadyExistsCount;

        public int EmailAddressDoesNotExistCount => NewRegistrationSyncOperation.EmailAddressDoesNotExistCount +
                                                    CoreContactAttributeSyncOperation.EmailAddressDoesNotExistCount;

        public int FailureCount => NewRegistrationSyncOperation.FailureCount +
                                   CoreContactAttributeSyncOperation.FailureCount +
                                   ChildAgeAndGradeSyncOperation.RetryFailureCount;

        public int HubSpotApiRequestCount => NewRegistrationSyncOperation.HubSpotApiRequestCount +
                                             CoreContactAttributeSyncOperation.HubSpotApiRequestCount +
                                             ChildAgeAndGradeSyncOperation.HubSpotApiRequestCount;

        public IActivityChildAgeAndGradeCalculationOperation ChildAgeAndGradeCalculationOperation { get; set; }

        public IActivitySyncOperation NewRegistrationSyncOperation { get; set; }

        public IActivitySyncOperation CoreContactAttributeSyncOperation { get; set; }

        public IActivityChildAgeAndGradeSyncOperation ChildAgeAndGradeSyncOperation { get; set; }

        public ActivityProgress ActivityProgress { get; set; }
    }
}