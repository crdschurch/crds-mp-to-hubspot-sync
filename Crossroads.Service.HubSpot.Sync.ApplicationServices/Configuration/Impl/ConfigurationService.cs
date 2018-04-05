﻿using System;
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

        public DateTime GetLastSuccessfulSyncDate()
        {
            _logger.LogInformation("Fetching last successful sync date...");

            var storedDateTime = _liteDbConfigurationProvider.Get<LastSuccessfulSyncDate, DateTime>();
            var syncDate = storedDateTime != default(DateTime)
                ? storedDateTime
                : _inauguralSync.Date;

            _logger.LogInformation($"Last successful sync date: {syncDate}");

            return syncDate;
        }

        public JobProcessingState GetCurrentJobProcessingState()
        {
            _logger.LogInformation("Checking job processing state...");
            return _liteDbConfigurationProvider.Get<JobProcessingStatus, JobProcessingState>();
        }

        public string GetEnvironmentName()
        {
            return _configurationRoot["ASPNETCORE_ENVIRONMENT"]; // environment variable
        }
    }
}