using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface IActivityResult : IPersist<string>
    {
        ExecutionTime Execution { get; }

        DateTime LastSuccessfulSyncDate { get; set; }

        int TotalContacts { get; set; }

        List<BulkRunResult> BulkRunResults { get; set; }

        SerialRunResult SerialRunResult { get; set; }

        JobProcessingState JobProcessingState { get; set; }
    }
}
