using System;

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

        int EmailAddressAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        SerialSyncResult SerialUpdateResult { get; set; }

        SerialSyncResult RetryEmailExistsAsSerialUpdateResult { get; set; }
    }
}
