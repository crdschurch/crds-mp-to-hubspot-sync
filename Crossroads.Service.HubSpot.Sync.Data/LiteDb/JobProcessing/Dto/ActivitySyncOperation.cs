using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Captures results (stats, errors, etc) around the operation to synchronize updated
    /// Ministry Platform contact data to HubSpot.
    /// </summary>
    public class ActivitySyncOperation : IActivitySyncOperation
    {
        public ActivitySyncOperation()
        {
            SerialUpdateResult = new SerialSyncResult();
            SerialCreateResult = new SerialSyncResult();
            SerialReconciliationResult = new SerialSyncResult();
        }

        public ActivitySyncOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            SerialUpdateResult = new SerialSyncResult();
            SerialCreateResult = new SerialSyncResult();
            SerialReconciliationResult = new SerialSyncResult();
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => SerialCreateResult.TotalContacts + SerialUpdateResult.TotalContacts; // a little strange, but necessary (leaky abstraction?)

        public int SuccessCount => UpdateSuccessCount + CreateSuccessCount + ReconciliationSuccessCount;

        public int FailureCount => UpdateFailureCount + CreateFailureCount + ReconciliationFailureCount;

        public int UpdateSuccessCount => SerialUpdateResult.SuccessCount;

        public int CreateSuccessCount => SerialCreateResult.SuccessCount;

        public int ReconciliationSuccessCount => SerialReconciliationResult.SuccessCount;

        public int UpdateFailureCount => SerialUpdateResult.FailureCount;

        public int CreateFailureCount => SerialCreateResult.FailureCount;

        public int ReconciliationFailureCount => SerialReconciliationResult.FailureCount;

        public int EmailAddressAlreadyExistsCount => SerialUpdateResult.EmailAddressAlreadyExistsCount +
                                                     SerialCreateResult.EmailAddressAlreadyExistsCount +
                                                     SerialReconciliationResult.EmailAddressAlreadyExistsCount;

        public int EmailAddressDoesNotExistCount => SerialUpdateResult.EmailAddressDoesNotExistCount +
                                                    SerialCreateResult.EmailAddressDoesNotExistCount +
                                                    SerialReconciliationResult.EmailAddressDoesNotExistCount;

        public int HubSpotApiRequestCount => SerialUpdateResult.SuccessCount +
                                             SerialUpdateResult.FailureCount +
                                             SerialUpdateResult.EmailAddressAlreadyExistsCount +
                                             SerialUpdateResult.EmailAddressDoesNotExistCount +
                                             SerialCreateResult.SuccessCount +
                                             SerialCreateResult.FailureCount +
                                             SerialCreateResult.EmailAddressAlreadyExistsCount +
                                             SerialReconciliationResult.SuccessCount +
                                             SerialReconciliationResult.FailureCount +
                                             SerialReconciliationResult.EmailAddressAlreadyExistsCount +
                                             SerialReconciliationResult.EmailAddressDoesNotExistCount +
                                             SerialReconciliationResult.DeleteCount +
                                             SerialReconciliationResult.GetCount;

        public SerialSyncResult SerialUpdateResult { get; set; }

        public SerialSyncResult SerialCreateResult { get; set; }

        public SerialSyncResult SerialReconciliationResult { get; set; }
    }
}