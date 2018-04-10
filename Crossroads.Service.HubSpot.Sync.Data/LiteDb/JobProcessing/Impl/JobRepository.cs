using System;
using System.Collections.Generic;
using System.Linq;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration;
using Crossroads.Service.HubSpot.Sync.LiteDB;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Impl
{
    public class JobRepository : IJobRepository
    {
        private readonly ILiteDbRepository _liteDbRepository;
        private readonly IClock _clock;
        private readonly ILiteDbConfigurationProvider _liteDbConfigurationProvider;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(ILiteDbRepository liteDbRepository, IClock clock, ILiteDbConfigurationProvider liteDbConfigurationProvider, ILogger<JobRepository> logger)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _liteDbConfigurationProvider = liteDbConfigurationProvider ?? throw new ArgumentNullException(nameof(liteDbConfigurationProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool SetLastSuccessfulSyncDate(SyncDates syncDates)
        {
            return _liteDbRepository.Upsert(new LastSuccessfulSyncDateInfo {Value = syncDates, LastUpdated = _clock.UtcNow});
        }

        public SyncProcessingState SetSyncJobProcessingState(SyncProcessingState syncProcessingState)
        {
            _liteDbRepository.Upsert(new SyncProcessingStatus { Value = syncProcessingState, LastUpdated = _clock.UtcNow});
            _logger.LogInformation($"Job is now in '{syncProcessingState}' state.");
            return _liteDbConfigurationProvider.Get<SyncProcessingStatus, SyncProcessingState>();
        }

        public bool SaveSyncActivity(ISyncActivity syncActivity)
        {
            syncActivity.LastUpdated = _clock.UtcNow;
            _logger.LogInformation("Storing sync activity...");
            return _liteDbRepository.Upsert(syncActivity);
        }

        public bool SaveHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime)
        {
            var previousRequestStats = _liteDbRepository.SingleOrDefault<HubSpotApiDailyRequestCount>(rq => rq.Date == activityDateTime.Date);
            _logger.LogInformation($"Previous request count: {previousRequestStats.Value}");

            var toPersist = new HubSpotApiDailyRequestCount
            {
                Value = previousRequestStats.Value + mostRecentRequestCount,
                Date = activityDateTime.Date,
                LastUpdated = _clock.UtcNow,
                TimesUpdated = ++previousRequestStats.TimesUpdated
            };

            _logger.LogInformation($"Current request count: {toPersist.Value}");

            return _liteDbRepository.Upsert(toPersist);
        }

        public List<HubSpotApiDailyRequestCount> GetHubSpotApiDailyRequestCount()
        {
            return _liteDbRepository.Fetch<HubSpotApiDailyRequestCount>();
        }

        public ISyncActivity GetActivity(string activityId)
        {
            return _liteDbRepository.SingleById<ISyncActivity>(activityId);
        }

        public ISyncActivity GetMostRecentActivity()
        {
            var mostRecentActivityId = _liteDbRepository.Database.GetCollection<ISyncActivity>().Max();
            return GetActivity(mostRecentActivityId.AsString);
        }

        public List<string> GetActivityIds(int limit)
        {
            return _liteDbRepository.Fetch<ISyncActivity>().Select(item => item.Id).ToList();
        }
    }
}
