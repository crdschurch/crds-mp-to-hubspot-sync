using System;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

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
            EmailChangedSyncResult = new CoreUpdateResult<EmailAddressChangedContact>();
            RetryEmailChangeAsCreateSyncResult = new SerialCreateSyncResult<EmailAddressCreatedContact>();
            CoreUpdateSyncResult = new CoreUpdateResult<CoreOnlyChangedContact>();
            RetryCoreUpdateAsCreateSyncResult = new SerialCreateSyncResult<EmailAddressCreatedContact>();
        }

        public SyncActivityCoreUpdateOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            EmailChangedSyncResult = new CoreUpdateResult<EmailAddressChangedContact>();
            RetryEmailChangeAsCreateSyncResult = new SerialCreateSyncResult<EmailAddressCreatedContact>();
            CoreUpdateSyncResult = new CoreUpdateResult<CoreOnlyChangedContact>();
            RetryCoreUpdateAsCreateSyncResult = new SerialCreateSyncResult<EmailAddressCreatedContact>();
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => EmailChangedSyncResult.TotalContacts +
                                    CoreUpdateSyncResult.TotalContacts;

        public int SuccessCount => InitialSuccessCount + RetrySuccessCount;

        public int InitialSuccessCount => EmailChangedSyncResult.SuccessCount +
                                          CoreUpdateSyncResult.SuccessCount;

        public int RetrySuccessCount => RetryEmailChangeAsCreateSyncResult.SuccessCount +
                                        RetryCoreUpdateAsCreateSyncResult.SuccessCount;

        public int InitialFailureCount => EmailChangedSyncResult.FailureCount +
                                          CoreUpdateSyncResult.FailureCount;

        public int RetryFailureCount =>   RetryEmailChangeAsCreateSyncResult.FailureCount +
                                          RetryCoreUpdateAsCreateSyncResult.FailureCount;

        public int ContactDoesNotExistCount => EmailChangedSyncResult.ContactDoesNotExistCount +
                                               CoreUpdateSyncResult.ContactDoesNotExistCount;

        public int ContactAlreadyExistsCount => RetryEmailChangeAsCreateSyncResult.ContactAlreadyExistsCount +
                                                RetryCoreUpdateAsCreateSyncResult.ContactAlreadyExistsCount;

        public int HubSpotApiRequestCount => EmailChangedSyncResult.SuccessCount +
                                             EmailChangedSyncResult.FailureCount +
                                             EmailChangedSyncResult.ContactDoesNotExistCount +
                                             RetryEmailChangeAsCreateSyncResult.SuccessCount +
                                             RetryEmailChangeAsCreateSyncResult.FailureCount +
                                             RetryEmailChangeAsCreateSyncResult.ContactAlreadyExistsCount +
                                             CoreUpdateSyncResult.SuccessCount +
                                             CoreUpdateSyncResult.FailureCount +
                                             CoreUpdateSyncResult.ContactDoesNotExistCount +
                                             RetryCoreUpdateAsCreateSyncResult.SuccessCount +
                                             RetryCoreUpdateAsCreateSyncResult.FailureCount +
                                             RetryCoreUpdateAsCreateSyncResult.ContactAlreadyExistsCount;

        public CoreUpdateResult<EmailAddressChangedContact> EmailChangedSyncResult { get; set; }

        public SerialCreateSyncResult<EmailAddressCreatedContact> RetryEmailChangeAsCreateSyncResult { get; set; }

        public CoreUpdateResult<CoreOnlyChangedContact> CoreUpdateSyncResult { get; set; }

        public SerialCreateSyncResult<EmailAddressCreatedContact> RetryCoreUpdateAsCreateSyncResult { get; set; }
    }
}
