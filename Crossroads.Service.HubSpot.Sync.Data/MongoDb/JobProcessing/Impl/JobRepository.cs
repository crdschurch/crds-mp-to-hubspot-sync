using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Impl
{
    public class JobRepository : IJobRepository
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IClock _clock;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(
            IMongoDatabase mongoDatabase,
            IClock clock,
            IJsonSerializer jsonSerializer,
            ILogger<JobRepository> logger)
        {
            _mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public OperationDates PersistLastSuccessfulOperationDates(OperationDates operationDates)
        {
            _mongoDatabase
                .GetCollection<OperationDatesKeyValue>(nameof(OperationDatesKeyValue))
                .ReplaceOne(
                    filter: Builders<OperationDatesKeyValue>.Filter.Eq("_id", nameof(OperationDatesKeyValue)),
                    replacement: new OperationDatesKeyValue { LastUpdatedUtc = _clock.UtcNow, Value = operationDates },
                    options: new UpdateOptions {IsUpsert = true});

            return operationDates;
        }

        public void PersistActivityProgress(ActivityProgress activityProgress)
        {
            _mongoDatabase
                .GetCollection<ActivityProgressKeyValue>(nameof(ActivityProgressKeyValue))
                .ReplaceOne(
                    filter: Builders<ActivityProgressKeyValue>.Filter.Eq("_id", nameof(ActivityProgressKeyValue)),
                    replacement: new ActivityProgressKeyValue { LastUpdatedUtc = _clock.UtcNow, Value = activityProgress },
                    options: new UpdateOptions { IsUpsert = true });
        }

        public void PersistActivity(Activity activity)
        {
            activity.LastUpdatedUtc = _clock.UtcNow;
            _logger.LogInformation("Storing activity...");
            _mongoDatabase.GetCollection<Activity>(nameof(Activity)).InsertOne(activity);
        }

        public void PersistHubSpotApiDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime)
        {
            var previousRequestStats =
                _mongoDatabase
                    .GetCollection<HubSpotApiDailyRequestCountKeyValue>(nameof(HubSpotApiDailyRequestCountKeyValue))
                    .Find(kv => kv.Date == activityDateTime.Date)
                    .FirstOrDefault() ?? new HubSpotApiDailyRequestCountKeyValue();
            _logger.LogInformation($"Previous request count: {previousRequestStats.Value}");

            var toPersist = new HubSpotApiDailyRequestCountKeyValue
            {
                Value = previousRequestStats.Value + mostRecentRequestCount,
                Date = activityDateTime.Date,
                LastUpdatedUtc = _clock.UtcNow,
                TimesUpdated = ++previousRequestStats.TimesUpdated
            };

            _logger.LogInformation($"Current request count: {toPersist.Value}");

            _mongoDatabase
                .GetCollection<HubSpotApiDailyRequestCountKeyValue>(nameof(HubSpotApiDailyRequestCountKeyValue))
                .ReplaceOne(
                    filter: Builders<HubSpotApiDailyRequestCountKeyValue>.Filter.Eq(nameof(HubSpotApiDailyRequestCountKeyValue.Date), activityDateTime.Date),
                    replacement: toPersist,
                    options: new UpdateOptions { IsUpsert = true });
        }

        public List<HubSpotApiDailyRequestCountKeyValue> GetHubSpotApiDailyRequestCount()
        {
            return _mongoDatabase
                .GetCollection<HubSpotApiDailyRequestCountKeyValue>(nameof(HubSpotApiDailyRequestCountKeyValue))
                .Find(getEverySingleDailyCount => true) // hack to get everything
                .ToList();
        }

        public string GetActivity(string activityId)
        {
            return _jsonSerializer.Serialize(
                _mongoDatabase
                    .GetCollection<Activity>(nameof(Activity))
                    .Find(kv => kv.Id == activityId)
                    .FirstOrDefault());
        }

        public string GetMostRecentActivity()
        {
            var mostRecentActivity = _mongoDatabase.GetCollection<Activity>(nameof(Activity)).Aggregate().Sort("{_id: -1}").First();
            return GetActivity(mostRecentActivity.Id);
        }

        public List<string> GetActivityIds(int limit)
        {
            return _mongoDatabase
                .GetCollection<Activity>(nameof(Activity))
                .Find(all => true)
                .Sort("{_id: -1}")
                .Limit(limit)
                .ToEnumerable()
                .Select(item => item.Id)
                .ToList();
        }
    }
}
