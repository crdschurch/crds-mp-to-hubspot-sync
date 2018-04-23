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
        }

        public SyncActivityCoreUpdateOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => EmailCreatedSyncResult?.TotalContacts ?? 0 +
                                    EmailChangedSyncResult?.TotalContacts ?? 0 +
                                    CoreUpdateSyncResult?.TotalContacts ?? 0;

        public int InitialSuccessCount =>  EmailCreatedSyncResult?.SuccessCount ?? 0 +
                                           EmailChangedSyncResult?.SuccessCount ?? 0 +
                                           CoreUpdateSyncResult?.SuccessCount ?? 0;

        public int RetrySuccessCount => RetryFailedCreationAsCoreUpdateResult?.SuccessCount ?? 0 +
                                        RetryFailedEmailChangeAsCreateResult?.SuccessCount ?? 0 +
                                        RetryFailedCoreUpdateAsCreateResult?.SuccessCount ?? 0;

        public int InitialFailureCount =>   EmailCreatedSyncResult?.FailureCount ?? 0 +
                                            EmailChangedSyncResult?.FailureCount ?? 0 +
                                            CoreUpdateSyncResult?.FailureCount ?? 0;

        public int RetryFailureCount =>   RetryFailedCreationAsCoreUpdateResult?.FailureCount ?? 0 +
                                          RetryFailedEmailChangeAsCreateResult?.FailureCount ?? 0 +
                                          RetryFailedCoreUpdateAsCreateResult?.FailureCount ?? 0;

        public int ContactDoesNotExistCount => RetryFailedCreationAsCoreUpdateResult?.ContactDoesNotExistCount ?? 0 +
                                               EmailChangedSyncResult?.ContactDoesNotExistCount ?? 0 +
                                               CoreUpdateSyncResult?.ContactDoesNotExistCount ?? 0;

        public int ContactAlreadyExistsCount => EmailCreatedSyncResult?.ContactAlreadyExistsCount ?? 0 +
                                                RetryFailedEmailChangeAsCreateResult?.ContactAlreadyExistsCount ?? 0 +
                                                RetryFailedCoreUpdateAsCreateResult?.ContactAlreadyExistsCount ?? 0;

        public int HubSpotApiRequestCount => EmailCreatedSyncResult?.TotalContacts ?? 0 +
                                        RetryFailedCreationAsCoreUpdateResult?.TotalContacts ?? 0 +
                                        EmailChangedSyncResult?.TotalContacts ?? 0 +
                                        RetryFailedEmailChangeAsCreateResult?.TotalContacts ?? 0 +
                                        CoreUpdateSyncResult?.TotalContacts ?? 0 +
                                        RetryFailedCoreUpdateAsCreateResult?.TotalContacts ?? 0;

        public SerialCreateSyncResult<EmailAddressCreatedContact> EmailCreatedSyncResult { get; set; }

        public CoreUpdateResult<CoreOnlyChangedContact> RetryFailedCreationAsCoreUpdateResult { get; set; }

        public CoreUpdateResult<EmailAddressChangedContact> EmailChangedSyncResult { get; set; }

        public SerialCreateSyncResult<EmailAddressCreatedContact> RetryFailedEmailChangeAsCreateResult { get; set; }

        public CoreUpdateResult<CoreOnlyChangedContact> CoreUpdateSyncResult { get; set; }

        public SerialCreateSyncResult<EmailAddressCreatedContact> RetryFailedCoreUpdateAsCreateResult { get; set; }
    }
}
