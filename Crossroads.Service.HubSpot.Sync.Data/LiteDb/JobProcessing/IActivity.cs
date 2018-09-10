using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    /// <summary>
    /// The overarching activity and aggregate root for getting contact
    /// data from Ministry Platform (MP) into HubSpot.
    /// 
    /// 4 distinct operations execute in order to move newly registered
    /// contacts, changes to core contact attributes and child age/grade
    /// data by household over to HubSpot. This object also captures the
    /// execution results (stats, errors, etc) around the sync job.
    /// </summary>
    public interface IActivity : IPersist<string>, ISyncResult
    {
        OperationDates PreviousOperationDates { get; set; }

        int EmailAddressAlreadyExistsCount { get; }

        int EmailAddressDoesNotExistCount { get; }

        int HubSpotApiRequestCount { get; }

        IActivityChildAgeAndGradeCalculationOperation ChildAgeAndGradeCalculationOperation { get; set; }

        IActivitySyncOperation NewRegistrationSyncOperation { get; set; }

        IActivitySyncOperation CoreContactAttributeSyncOperation { get; set; }

        IActivityChildAgeAndGradeSyncOperation ChildAgeAndGradeSyncOperation { get; set; }

        ActivityProgress ActivityProgress { get; set; }
    }
}
