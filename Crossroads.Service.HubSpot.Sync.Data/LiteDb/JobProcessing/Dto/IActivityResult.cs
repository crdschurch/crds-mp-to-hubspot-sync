using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface IActivityResult : IPersist<string>
    {
        ExecutionTime Execution { get; }

        DateTime LastSuccessfulSyncDate { get; set; }

        int TotalContacts { get; set; }

        List<BulkActivityResult> BulkRunResults { get; set; }

        SerialActivityResult SerialRunResult { get; set; }
    }
}
