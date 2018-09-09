
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IJobRepository
    {
        SyncDates PersistLastSuccessfulSyncDates(SyncDates syncDates);

        void SetSyncProgress(SyncProgress syncProgress);

        bool SaveSyncActivity(ISyncActivity syncActivity);

        bool SaveHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime);

        List<HubSpotApiDailyRequestCountKeyValue> GetHubSpotApiDailyRequestCount();

        string GetActivity(string syncJobActivityId);

        string GetMostRecentActivity();

        List<string> GetActivityIds(int limit);
    }
}
