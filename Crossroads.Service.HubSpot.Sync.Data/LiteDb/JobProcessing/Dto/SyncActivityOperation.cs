using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncActivityOperation : ISyncActivityOperation
    {
        public SyncActivityOperation()
        {
        }

        public SyncActivityOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
        }

        public IExecutionTime Execution { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public int TotalContacts { get; set; }

        public BulkSyncResult BulkSyncResult { get; set; }

        public SerialSyncResult SerialSyncResult { get; set; }
    }
}
