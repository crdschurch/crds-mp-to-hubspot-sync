using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration
{
    public interface IConfigurationService
    {
        SyncDates GetLastSuccessfulSyncDates();

        SyncProcessingState GetCurrentJobProcessingState();

        string GetEnvironmentName();
    }
}
