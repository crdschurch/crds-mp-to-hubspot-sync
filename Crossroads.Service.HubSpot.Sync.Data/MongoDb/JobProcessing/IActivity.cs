using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing
{
    /// <summary>
    /// The overarching activity and aggregate root for getting contact
    /// data from Ministry Platform (MP) into HubSpot.
    /// 
    /// 4 distinct operations execute in order to move newly registered
    /// contacts, changes to core contact attributes and child age/grade
    /// data by household over to HubSpot. This object also captures the
    /// execution results (stats, errors, retries, etc) around the sync job.
    /// </summary>
    public interface IActivity : IPersist<string>, ISyncResult
    {
        OperationDates PreviousOperationDates { get; set; }

        int EmailAddressAlreadyExistsCount { get; }

        int EmailAddressDoesNotExistCount { get; }

        int HubSpotApiRequestCount { get; }

        ActivityChildAgeAndGradeCalculationOperation ChildAgeAndGradeCalculationOperation { get; set; }

        ActivitySyncOperation NewRegistrationSyncOperation { get; set; }

        ActivitySyncOperation CoreContactAttributeSyncOperation { get; set; }
        
        ActivityChildAgeAndGradeSyncOperation ChildAgeAndGradeSyncOperation { get; set; }

        ActivityProgress ActivityProgress { get; set; }
    }
}
