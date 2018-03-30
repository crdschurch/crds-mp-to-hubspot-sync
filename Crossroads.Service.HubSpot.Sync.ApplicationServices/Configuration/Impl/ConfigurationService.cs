using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration;
using Microsoft.Extensions.Configuration;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILiteDbConfigurationProvider _liteDbConfigurationProvider;
        private readonly IConfigurationRoot _appSettings;

        public ConfigurationService(ILiteDbConfigurationProvider liteDbConfigurationProvider, IConfigurationRoot appSettings)
        {
            _liteDbConfigurationProvider = liteDbConfigurationProvider ?? throw new ArgumentNullException(nameof(liteDbConfigurationProvider));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        public DateTime GetLastSuccessfulSyncDate()
        {
            var key = default(LastSuccessfulSyncDate).Key;
            var storedDateTime = _liteDbConfigurationProvider.GetConfiguration<LastSuccessfulSyncDate, DateTime>();
            return (storedDateTime != default(DateTime)) ? storedDateTime : DateTime.Parse(_appSettings[key]);
        }

        public JobProcessingState GetCurrentJobProcessingState()
        {
            return _liteDbConfigurationProvider.GetConfiguration<JobProcessingStatus, JobProcessingState>();
        }
    }
}