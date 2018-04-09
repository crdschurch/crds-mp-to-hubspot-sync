using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface ISyncActivityOperation
    {
        IExecutionTime Execution { get; }

        DateTime PreviousSyncDate { get; set; }

        int TotalContacts { get; set; }

        BulkSyncResult BulkSyncResult { get; set; }

        SerialSyncResult SerialSyncResult { get; set; }
    }
}
