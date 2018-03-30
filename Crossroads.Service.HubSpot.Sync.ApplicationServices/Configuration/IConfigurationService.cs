using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration
{
    public interface IConfigurationService
    {
        DateTime GetLastSuccessfulSyncDate();

        JobProcessingState GetCurrentJobProcessingState();
    }
}
