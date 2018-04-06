using System;
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

        public bool SetLastSuccessfulSyncDate(DateTime dateTime)
        {
            return _liteDbRepository.Upsert(new LastSuccessfulSyncDate {Value = dateTime, LastUpdated = _clock.UtcNow});
        }

        public JobProcessingState SetJobProcessingState(JobProcessingState jobProcessingState)
        {
            _liteDbRepository.Upsert(new JobProcessingStatus { Value = jobProcessingState, LastUpdated = _clock.UtcNow});
            _logger.LogInformation($"Job is now in '{jobProcessingState}' state.");
            return _liteDbConfigurationProvider.Get<JobProcessingStatus, JobProcessingState>();
        }

        public bool SaveActivityResult(IActivityResult activityResult)
        {
            activityResult.LastUpdated = _clock.UtcNow;
            _logger.LogInformation("Storing job activity...");
            return _liteDbRepository.Upsert(activityResult);
        }

        public bool SaveHubSpotDailyRequestCount(int mostRecentRequestCount, DateTime activityDateTime)
        {
            var previousRequestStats = _liteDbRepository.SingleOrDefault<HubSpotDailyRequestCount>(rq => rq.Date == activityDateTime.Date);
            _logger.LogInformation($"Previous request count: {previousRequestStats.Value}");

            var toPersist = new HubSpotDailyRequestCount
            {
                Value = previousRequestStats.Value + mostRecentRequestCount,
                Date = activityDateTime.Date,
                LastUpdated = _clock.UtcNow,
                TimesUpdated = ++previousRequestStats.TimesUpdated
            };

            _logger.LogInformation($"Current request count: {toPersist.Value}");

            return _liteDbRepository.Upsert(toPersist);
        }

        public IActivityResult GetActivity(string activityId)
        {
            var activity = _liteDbRepository.SingleById<IActivityResult>(activityId);
            return activity;
        }
    }
}
