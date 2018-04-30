using System;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILiteDbConfigurationProvider _liteDbConfigurationProvider;
        private readonly IConfigurationRoot _configurationRoot;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly InauguralSync _inauguralSync;

        public ConfigurationService(ILiteDbConfigurationProvider liteDbConfigurationProvider,
            IConfigurationRoot configurationRoot,
            IOptions<InauguralSync> inauguralSync,
            ILogger<ConfigurationService> logger)
        {
            _liteDbConfigurationProvider = liteDbConfigurationProvider ?? throw new ArgumentNullException(nameof(liteDbConfigurationProvider));
            _configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inauguralSync = inauguralSync?.Value ?? throw new ArgumentNullException(nameof(inauguralSync));
        }

        public SyncDates GetLastSuccessfulSyncDates()
        {
            _logger.LogInformation("Fetching last successful sync date...");

            var syncDates = _liteDbConfigurationProvider.Get<LastSuccessfulSyncDateInfo, SyncDates>();
            if(syncDates.RegistrationSyncDate == default(DateTime)) // if this is true, we've never run for new MP registrations
                syncDates.RegistrationSyncDate = _inauguralSync.RegistrationSyncDate;

            if (syncDates.CoreUpdateSyncDate == default(DateTime)) // if this is true, we've never run for core MP contact updates
                syncDates.CoreUpdateSyncDate = _inauguralSync.CoreUpdateSyncDate;

            _logger.LogInformation($@"Last successful sync dates.
new registration: {syncDates.RegistrationSyncDate}
updates: {syncDates.CoreUpdateSyncDate}");

            return syncDates;
        }

        public SyncProcessingState GetCurrentJobProcessingState()
        {
            _logger.LogInformation("Checking job processing state...");
            return _liteDbConfigurationProvider.Get<SyncProcessingStatus, SyncProcessingState>();
        }

        public string GetEnvironmentName()
        {
            return _configurationRoot["ASPNETCORE_ENVIRONMENT"]; // environment variable
        }
    }
}