using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Captures results (stats, errors, etc) around the operation to synchronize updated
    /// Ministry Platform contact data to HubSpot.
    /// </summary>
    public class SyncActivityChildAgeAndGradeUpdateOperation : ISyncActivityChildAgeAndGradeUpdateOperation
    {
        public SyncActivityChildAgeAndGradeUpdateOperation()
        {
            BulkUpdateSyncResult100 = new BulkSyncResult();
            BulkUpdateSyncResult10 = new BulkSyncResult();
            RetryBulkUpdateAsSerialUpdateResult = new SerialSyncResult();
        }

        public SyncActivityChildAgeAndGradeUpdateOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            BulkUpdateSyncResult100 = new BulkSyncResult();
            BulkUpdateSyncResult10 = new BulkSyncResult();
            RetryBulkUpdateAsSerialUpdateResult = new SerialSyncResult();
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => BulkUpdateSyncResult100.TotalContacts;

        public int SuccessCount => InitialSuccessCount + RetrySuccessCount;

        public int InitialSuccessCount => BulkUpdateSyncResult100.SuccessCount;

        public int RetrySuccessCount => RetryBulkUpdateAsSerialUpdateResult.SuccessCount + BulkUpdateSyncResult10.SuccessCount;

        public int InitialFailureCount => BulkUpdateSyncResult100.FailureCount;

        public int RetryFailureCount => RetryBulkUpdateAsSerialUpdateResult.FailureCount;

        public int EmailAddressAlreadyExistsCount => RetryBulkUpdateAsSerialUpdateResult.EmailAddressAlreadyExistsCount;

        public int HubSpotApiRequestCount => BulkUpdateSyncResult100.BatchCount +
                                             BulkUpdateSyncResult10.BatchCount +
                                             RetryBulkUpdateAsSerialUpdateResult.SuccessCount +
                                             RetryBulkUpdateAsSerialUpdateResult.FailureCount;

        public ChildAgeAndGradeDeltaLogDto AgeAndGradeDelta { get; set; }

        public BulkSyncResult BulkUpdateSyncResult100 { get; set; }

        public BulkSyncResult BulkUpdateSyncResult10 { get; set; }

        public SerialSyncResult RetryBulkUpdateAsSerialUpdateResult { get; set; }
    }
}
