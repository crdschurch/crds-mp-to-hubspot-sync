using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Represents the activity for updating core HubSpot properties:
    /// First name, Last name, Email, Community, Marital Status. It also
    /// captures results (stats, errors, etc) around the sync operation.
    /// </summary>
    public interface ISyncActivityOperation
    {
        IExecutionTime Execution { get; }

        DateTime PreviousSyncDate { get; set; }

        int TotalContacts { get; }

        int SuccessCount { get; }

        int FailureCount { get; }

        int UpdateSuccessCount { get; }

        int CreateSuccessCount { get; }

        int ReconciliationSuccessCount { get; }

        int UpdateFailureCount { get; }

        int CreateFailureCount { get; }

        int ReconciliationFailureCount { get; }

        int EmailAddressAlreadyExistsCount { get; }

        int EmailAddressDoesNotExistCount { get; }

        int HubSpotApiRequestCount { get; }

        SerialSyncResult SerialUpdateResult { get; set; }

        SerialSyncResult SerialCreateResult { get; set; }

        SerialSyncResult SerialReconciliationResult { get; set; }
    }
}
