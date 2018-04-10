
using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IJobRepository
    {
        bool SetLastSuccessfulSyncDate(SyncDates syncDates);

        SyncProcessingState SetSyncJobProcessingState(SyncProcessingState syncProcessingState);

        bool SaveSyncActivity(ISyncActivity syncActivity);

        bool SaveHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime);

        List<HubSpotApiDailyRequestCount> GetHubSpotApiDailyRequestCount();

        ISyncActivity GetActivity(string syncJobActivityId);

        ISyncActivity GetMostRecentActivity();

        List<string> GetActivityIds(int limit);
    }
}
