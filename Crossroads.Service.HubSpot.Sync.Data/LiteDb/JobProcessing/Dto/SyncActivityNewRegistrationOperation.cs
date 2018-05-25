using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Captures results (stats, errors, etc) around the operation to synchronize newly
    /// registered Ministry Platform contacts to HubSpot.
    /// </summary>
    public class SyncActivityNewRegistrationOperation : ISyncActivityNewRegistrationOperation
    {
        public SyncActivityNewRegistrationOperation()
        {
            BulkCreateSyncResult = new BulkSyncResult();
            SerialCreateSyncResult = new SerialSyncResult();
        }

        public SyncActivityNewRegistrationOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            BulkCreateSyncResult = new BulkSyncResult();
            SerialCreateSyncResult = new SerialSyncResult();
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => BulkCreateSyncResult.TotalContacts;

        public int SuccessCount => BulkCreateSyncResult.SuccessCount + SerialCreateSyncResult.SuccessCount;

        public int EmailAddressAlreadyExistsCount => SerialCreateSyncResult.EmailAddressAlreadyExistsCount;

        public int FailureCount => SerialCreateSyncResult.FailureCount;

        public int HubSpotApiRequestCount => BulkCreateSyncResult.BatchCount +
                                             SerialCreateSyncResult.SuccessCount +
                                             SerialCreateSyncResult.FailureCount +
                                             SerialCreateSyncResult.EmailAddressAlreadyExistsCount;

        public BulkSyncResult BulkCreateSyncResult { get; set; }

        public SerialSyncResult SerialCreateSyncResult { get; set; }
    }
}
