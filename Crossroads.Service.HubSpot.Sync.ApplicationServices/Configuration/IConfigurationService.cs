using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration
{
    public interface IConfigurationService
    {
        OperationDates GetLastSuccessfulOperationDates();

        /// <summary>
        /// On the initial sync run, it returns a new instance of SyncProgress with a JobState of Idle. All
        /// subsequent syncs will pull from the data store.
        /// </summary>
        ActivityProgress GetCurrentActivityProgress();

        string GetEnvironmentName();

        bool PersistActivity();
    }
}
