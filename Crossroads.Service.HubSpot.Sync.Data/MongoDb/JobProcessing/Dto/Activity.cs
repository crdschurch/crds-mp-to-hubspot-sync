using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class Activity : IActivity
    {
        public Activity()
        {
            PreviousOperationDates = new OperationDates();
            ChildAgeAndGradeCalculationOperation = new ActivityChildAgeAndGradeCalculationOperation();
            NewRegistrationSyncOperation = new ActivitySyncOperation();
            CoreContactAttributeSyncOperation = new ActivitySyncOperation();
            ChildAgeAndGradeSyncOperation = new ActivityChildAgeAndGradeSyncOperation();
            ActivityProgress = new ActivityProgress();
        }

        public Activity(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            Id = $"{nameof(Activity)}_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
            PreviousOperationDates = new OperationDates();
            ChildAgeAndGradeCalculationOperation = new ActivityChildAgeAndGradeCalculationOperation();
            NewRegistrationSyncOperation = new ActivitySyncOperation();
            CoreContactAttributeSyncOperation = new ActivitySyncOperation();
            ChildAgeAndGradeSyncOperation = new ActivityChildAgeAndGradeSyncOperation();
            ActivityProgress = new ActivityProgress();
        }

        [BsonElement("_id")]
        public string Id { get; set; }

        public DateTime LastUpdatedUtc { get; set; }

        public ExecutionTime Execution { get; set; }

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

        public ActivityChildAgeAndGradeCalculationOperation ChildAgeAndGradeCalculationOperation { get; set; }

        public ActivitySyncOperation NewRegistrationSyncOperation { get; set; }

        public ActivitySyncOperation CoreContactAttributeSyncOperation { get; set; }

        public ActivityChildAgeAndGradeSyncOperation ChildAgeAndGradeSyncOperation { get; set; }

        public ActivityProgress ActivityProgress { get; set; }
    }
}