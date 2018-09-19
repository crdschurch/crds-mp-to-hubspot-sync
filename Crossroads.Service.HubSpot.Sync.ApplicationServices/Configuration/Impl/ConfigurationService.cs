using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IConfigurationRoot _configurationRoot;
        private readonly DocumentDbSettings _documentDbSettings;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly InauguralSync _inauguralSync;

        public ConfigurationService(
            IMongoDatabase mongoDatabase,
            IConfigurationRoot configurationRoot,
            IOptions<InauguralSync> inauguralSync,
            IOptions<DocumentDbSettings> documentDbSettings,
            ILogger<ConfigurationService> logger)
        {
            _mongoDatabase = mongoDatabase;
            _configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
            _documentDbSettings = documentDbSettings.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inauguralSync = inauguralSync?.Value ?? throw new ArgumentNullException(nameof(inauguralSync));
        }

        public OperationDates GetLastSuccessfulOperationDates()
        {
            _logger.LogInformation("Fetching last successful operation dates...");

            var operationDates = _mongoDatabase
                .GetCollection<OperationDatesKeyValue>(nameof(OperationDatesKeyValue))
                .Find(Builders<OperationDatesKeyValue>.Filter.Eq("_id", nameof(OperationDatesKeyValue)))
                .FirstOrDefault()?.Value ?? new OperationDates();

            if (operationDates.RegistrationSyncDate == default(DateTime)) // if this is true, we've never run for new MP registrations
                operationDates.RegistrationSyncDate = _inauguralSync.RegistrationSyncDate;

            if (operationDates.CoreUpdateSyncDate == default(DateTime)) // if this is true, we've never run for core MP contact updates
                operationDates.CoreUpdateSyncDate = _inauguralSync.CoreUpdateSyncDate;

            _logger.LogInformation($@"Last successful sync/process dates.
new registration: {operationDates.RegistrationSyncDate.ToLocalTime()}
core updates: {operationDates.CoreUpdateSyncDate.ToLocalTime()}
age and grade process: {operationDates.AgeAndGradeProcessDate.ToLocalTime()}
age and grade sync: {operationDates.AgeAndGradeSyncDate.ToLocalTime()}");

            return operationDates;
        }

        public ActivityProgress GetCurrentActivityProgress()
        {
            _logger.LogInformation("Fetching activity progress...");
            return _mongoDatabase
                       .GetCollection<ActivityProgressKeyValue>(nameof(ActivityProgressKeyValue))
                       .Find(Builders<ActivityProgressKeyValue>.Filter.Eq("_id", nameof(ActivityProgressKeyValue)))
                       .FirstOrDefault()?.Value ?? new ActivityProgress { ActivityState = ActivityState.Idle };
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