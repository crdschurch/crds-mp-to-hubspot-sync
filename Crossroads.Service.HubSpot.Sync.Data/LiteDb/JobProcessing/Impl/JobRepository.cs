using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
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
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(
            ILiteDbRepository liteDbRepository,
            IClock clock,
            IJsonSerializer jsonSerializer,
            ILogger<JobRepository> logger)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public OperationDates PersistLastSuccessfulOperationDates(OperationDates operationDates)
        {
            _liteDbRepository.Upsert(new LastSuccessfulOperationDateInfoKeyValue {Value = operationDates, LastUpdated = _clock.UtcNow});
            return operationDates;
        }

        public void PersistActivityProgress(ActivityProgress activityProgress)
        {
            _liteDbRepository.Upsert(new ActivityProgressKeyValue { Value = activityProgress, LastUpdated = _clock.UtcNow});
            _logger.LogInformation($"Job is now in '{activityProgress.ActivityState}' state.");
            _logger.LogInformation($"{string.Join("\r\n", activityProgress.Operations.Select(k => $"Step {k.Key}: {k.Value}"))}");
        }

        public bool PersistActivity(IActivity activity)
        {
            activity.LastUpdated = _clock.UtcNow;
            _logger.LogInformation("Storing activity...");
            return _liteDbRepository.Upsert(activity);
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
                return _jsonSerializer.Serialize(_liteDbRepository.SingleById<IActivity>(activityId));
            }
            catch
            {
                return _liteDbRepository.Engine.FindOne(nameof(IActivity), Query.EQ("_id", activityId)).ToString();
            }
        }

        public string GetMostRecentActivity()
        {
            var mostRecentActivityId = _liteDbRepository.Database.GetCollection<IActivity>().Max();
            return GetActivity(mostRecentActivityId.AsString);
        }

        public List<string> GetActivityIds(int limit)
        {
            return _liteDbRepository.Engine.Find(nameof(IActivity), Query.StartsWith("_id", nameof(Activity)))
                .Select(item => item.Values.First().AsString).ToList();
        }
    }
}
