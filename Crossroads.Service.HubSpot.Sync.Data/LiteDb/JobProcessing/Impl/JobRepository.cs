using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration;
using Crossroads.Service.HubSpot.Sync.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Impl
{
    public class JobRepository : IJobRepository
    {
        private readonly ILiteDbRepository _liteDbRepository;
        private readonly IClock _clock;
        private readonly ILiteDbConfigurationProvider _liteDbConfigurationProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(
            ILiteDbRepository liteDbRepository,
            IClock clock,
            ILiteDbConfigurationProvider liteDbConfigurationProvider,
            IJsonSerializer jsonSerializer,
            ILogger<JobRepository> logger)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _liteDbConfigurationProvider = liteDbConfigurationProvider ?? throw new ArgumentNullException(nameof(liteDbConfigurationProvider));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SyncDates PersistLastSuccessfulSyncDates(SyncDates syncDates)
        {
            _liteDbRepository.Upsert(new LastSuccessfulSyncDateInfoKeyValue {Value = syncDates, LastUpdated = _clock.UtcNow});
            return syncDates;
        }

        public void SetSyncProgress(SyncProgress syncProgress)
        {
            _liteDbRepository.Upsert(new SyncProgressKeyValue { Value = syncProgress, LastUpdated = _clock.UtcNow});
            _logger.LogInformation($"Job is now in '{syncProgress.SyncState}' state.");
            _logger.LogInformation($"{string.Join("\r\n", syncProgress.Steps.Select(k => $"Step {k.Key}: {k.Value}"))}");
        }

        public bool SaveSyncActivity(ISyncActivity syncActivity)
        {
            syncActivity.LastUpdated = _clock.UtcNow;
            _logger.LogInformation("Storing sync activity...");
            return _liteDbRepository.Upsert(syncActivity);
        }

        public bool SaveHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime)
        {
            var previousRequestStats = _liteDbRepository.SingleOrDefault<HubSpotApiDailyRequestCountKeyValue>(rq => rq.Date == activityDateTime.Date);
            _logger.LogInformation($"Previous request count: {previousRequestStats.Value}");

            var toPersist = new HubSpotApiDailyRequestCountKeyValue
            {
                Value = previousRequestStats.Value + mostRecentRequestCount,
                Date = activityDateTime.Date,
                LastUpdated = _clock.UtcNow,
                TimesUpdated = ++previousRequestStats.TimesUpdated
            };

            _logger.LogInformation($"Current request count: {toPersist.Value}");

            return _liteDbRepository.Upsert(toPersist);
        }

        public List<HubSpotApiDailyRequestCountKeyValue> GetHubSpotApiDailyRequestCount()
        {
            return _liteDbRepository.Fetch<HubSpotApiDailyRequestCountKeyValue>();
        }

        public string GetActivity(string activityId)
        {
            try
            {
                return _jsonSerializer.Serialize(_liteDbRepository.SingleById<ISyncActivity>(activityId));
            }
            catch
            {
                return _liteDbRepository.Engine.FindOne(nameof(ISyncActivity), Query.EQ("_id", activityId)).ToString();
            }
        }

        public string GetMostRecentActivity()
        {
            var mostRecentActivityId = _liteDbRepository.Database.GetCollection<ISyncActivity>().Max();
            return GetActivity(mostRecentActivityId.AsString);
        }

        public List<string> GetActivityIds(int limit)
        {
            return _liteDbRepository.Engine.Find(nameof(ISyncActivity), Query.StartsWith("_id", nameof(SyncActivity)))
                .Select(item => item.Values.First().AsString).ToList();
        }
    }
}
