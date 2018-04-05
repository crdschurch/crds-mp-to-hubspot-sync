
using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IJobRepository
    {
        bool SetLastSuccessfulSyncDate(DateTime dateTime);

        JobProcessingState SetJobProcessingState(JobProcessingState jobProcessingState);

        bool SaveActivityResult(IActivityResult activityResult);

        bool SaveHubSpotDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime);
    }
}
