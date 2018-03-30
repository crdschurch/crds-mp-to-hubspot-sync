
using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IJobRepository
    {
        bool SetLastSuccessfulSyncDate(DateTime dateTime);

        bool SetJobProcessingState(JobProcessingState jobProcessingState);

        bool StoreJobActivity(IActivity activity);
    }
}
