using System;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    /// <summary>
    /// Represents the activity for synchronizing newly registered Ministry Platform
    /// contacts to HubSpot. It also captures results (stats, errors, etc) around the
    /// sync operation.
    /// </summary>
    public interface ISyncActivityNewRegistrationOperation
    {
        IExecutionTime Execution { get; }

        DateTime PreviousSyncDate { get; set; }

        int TotalContacts { get; }

        int SuccessCount { get; }

        int ContactAlreadyExistsCount { get; }

        int FailureCount { get; }

        int HubSpotApiRequestCount { get; }

        BulkSyncResult BulkCreateSyncResult { get; set; }

        SerialCreateSyncResult<SerialCreateContact> SerialCreateSyncResult { get; set; }
    }
}
