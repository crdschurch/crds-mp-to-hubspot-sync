using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration
{
    public interface IConfigurationService
    {
        SyncDates GetLastSuccessfulSyncDates();

        /// <summary>
        /// On the initial sync run, it returns a new instance of SyncProgress with a JobState of Idle. All
        /// subsequent syncs will pull from the data store.
        /// </summary>
        SyncProgress GetCurrentSyncProgress();

        string GetEnvironmentName();

        bool PersistActivity();
    }
}
