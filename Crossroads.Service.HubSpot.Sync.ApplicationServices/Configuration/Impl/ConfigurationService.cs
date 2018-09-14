using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IMongoDbConfigurationProvider _mongoDbConfigurationProvider;
        private readonly IConfigurationRoot _configurationRoot;
        private readonly DocumentDbSettings _documentDbSettings;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly InauguralSync _inauguralSync;

        public ConfigurationService(
            IMongoDbConfigurationProvider mongoDbConfigurationProvider,
            IConfigurationRoot configurationRoot,
            IOptions<InauguralSync> inauguralSync,
            IOptions<DocumentDbSettings> documentDbSettings,
            ILogger<ConfigurationService> logger)
        {
            _mongoDbConfigurationProvider = mongoDbConfigurationProvider ?? throw new ArgumentNullException(nameof(mongoDbConfigurationProvider));
            _configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
            _documentDbSettings = documentDbSettings.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inauguralSync = inauguralSync?.Value ?? throw new ArgumentNullException(nameof(inauguralSync));
        }

        public OperationDates GetLastSuccessfulOperationDates()
        {
            _logger.LogInformation("Fetching last successful operation dates...");

            var syncDates = _mongoDbConfigurationProvider.Get<OperationDatesKeyValue, OperationDates>();
            if(syncDates.RegistrationSyncDate == default(DateTime)) // if this is true, we've never run for new MP registrations
                syncDates.RegistrationSyncDate = _inauguralSync.RegistrationSyncDate;

            if (syncDates.CoreUpdateSyncDate == default(DateTime)) // if this is true, we've never run for core MP contact updates
                syncDates.CoreUpdateSyncDate = _inauguralSync.CoreUpdateSyncDate;

            _logger.LogInformation($@"Last successful sync/process dates.
new registration: {syncDates.RegistrationSyncDate.ToLocalTime()}
core updates: {syncDates.CoreUpdateSyncDate.ToLocalTime()}
age and grade process: {syncDates.AgeAndGradeProcessDate.ToLocalTime()}
age and grade sync: {syncDates.AgeAndGradeSyncDate.ToLocalTime()}");

            return syncDates;
        }

        public ActivityProgress GetCurrentActivityProgress()
        {
            _logger.LogInformation("Fetching activity progress...");
            return _mongoDbConfigurationProvider.Get<ActivityProgressKeyValue, ActivityProgress>() ?? new ActivityProgress{ ActivityState = ActivityState.Idle };
        }

        public string GetEnvironmentName()
        {
            return _configurationRoot["ASPNETCORE_ENVIRONMENT"]; // environment variable
        }

        public bool PersistActivity()
        {
            return _documentDbSettings.PersistActivity;
        }
    }
}