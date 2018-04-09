using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface ISyncActivity : IPersist<string>
    {
        Guid SyncActivityId { get; }

        IExecutionTime Execution { get; }

        SyncDates PreviousSyncDates { get; set; }

        int TotalContacts { get; set; }

        ISyncActivityOperation CreateOperation { get; set; }

        ISyncActivityOperation UpdateOperation { get; set; }

        SyncProcessingState SyncProcessingState { get; set; }
    }
}
