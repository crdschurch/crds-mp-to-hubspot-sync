
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IJobRepository
    {
        OperationDates PersistLastSuccessfulOperationDates(OperationDates operationDates);

        void PersistActivityProgress(ActivityProgress activityProgress);

        bool PersistActivity(IActivity activity);

        bool SaveHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime);

        List<HubSpotApiDailyRequestCountKeyValue> GetHubSpotApiDailyRequestCount();

        string GetActivity(string syncJobActivityId);

        string GetMostRecentActivity();

        List<string> GetActivityIds(int limit);
    }
}
