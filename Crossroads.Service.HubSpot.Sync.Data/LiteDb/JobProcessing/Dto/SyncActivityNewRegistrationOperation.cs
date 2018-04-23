using System;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

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
        }

        public SyncActivityNewRegistrationOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts => BulkCreateSyncResult.TotalContacts;

        public int HubSpotApiRequestCount => BulkCreateSyncResult.TotalContacts + SerialCreateSyncResult?.TotalContacts ?? 0;

        public BulkSyncResult BulkCreateSyncResult { get; set; }

        public SerialCreateSyncResult<SerialCreateContact> SerialCreateSyncResult { get; set; }
    }
}
