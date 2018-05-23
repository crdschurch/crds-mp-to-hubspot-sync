using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Captures results (stats, errors, etc) around the operation to synchronize updated
    /// Ministry Platform contact data to HubSpot.
    /// </summary>
    public class SyncActivityCoreUpdateOperation : ISyncActivityCoreUpdateOperation
    {
        public SyncActivityCoreUpdateOperation()
        {
            SerialUpdateResult = new SerialSyncResult();
            RetryEmailExistsAsSerialUpdateResult = new SerialSyncResult();
        }

        public SyncActivityCoreUpdateOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            SerialUpdateResult = new SerialSyncResult();
            RetryEmailExistsAsSerialUpdateResult = new SerialSyncResult();
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => SerialUpdateResult.TotalContacts;

        public int SuccessCount => InitialSuccessCount + RetrySuccessCount;

        public int InitialSuccessCount => SerialUpdateResult.SuccessCount;

        public int RetrySuccessCount => RetryEmailExistsAsSerialUpdateResult.SuccessCount;

        public int InitialFailureCount => SerialUpdateResult.FailureCount;

        public int RetryFailureCount => RetryEmailExistsAsSerialUpdateResult.FailureCount;

        public int EmailAddressAlreadyExistsCount => SerialUpdateResult.EmailAddressAlreadyExistsCount +
                                                     RetryEmailExistsAsSerialUpdateResult.EmailAddressAlreadyExistsCount;

        public int HubSpotApiRequestCount => SerialUpdateResult.SuccessCount +
                                             SerialUpdateResult.FailureCount +
                                             SerialUpdateResult.EmailAddressAlreadyExistsCount +
                                             RetryEmailExistsAsSerialUpdateResult.SuccessCount +
                                             RetryEmailExistsAsSerialUpdateResult.FailureCount;

        public SerialSyncResult SerialUpdateResult { get; set; }

        public SerialSyncResult RetryEmailExistsAsSerialUpdateResult { get; set; }
    }
}
