
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing
{
    /// <summary>
    /// Holds sync job related settings and operational results.
    /// </summary>
    public interface IJobRepository
    {
        OperationDates PersistLastSuccessfulOperationDates(OperationDates operationDates);

        void PersistActivityProgress(ActivityProgress activityProgress);

        void PersistActivity(Activity activity);

        void PersistHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime);

        List<HubSpotApiDailyRequestCountKeyValue> GetHubSpotApiDailyRequestCount();

        string GetActivity(string syncJobActivityId);

        string GetMostRecentActivity();

        List<string> GetActivityIds(int limit);
    }
}
