using System;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Represents the activity for updating core HubSpot properties:
    /// First name, Last name, Email, Community, Marital Status. It also
    /// captures results (stats, errors, etc) around the sync operation.
    /// </summary>
    public interface ISyncActivityCoreUpdateOperation
    {
        IExecutionTime Execution { get; }

        DateTime PreviousSyncDate { get; set; }

        int TotalContacts { get; }

        int SuccessCount { get; }

        int InitialSuccessCount { get; }

        int RetrySuccessCount { get; }

        int InitialFailureCount { get; }

        int RetryFailureCount { get; }

        int ContactDoesNotExistCount { get; }

        int ContactAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        CoreUpdateResult<EmailAddressChangedContact> EmailChangedSyncResult { get; set; }

        SerialCreateSyncResult<EmailAddressCreatedContact> RetryEmailChangeAsCreateSyncResult { get; set; }

        CoreUpdateResult<CoreOnlyChangedContact> CoreUpdateSyncResult { get; set; }

        SerialCreateSyncResult<EmailAddressCreatedContact> RetryCoreUpdateAsCreateSyncResult { get; set; }
    }
}
