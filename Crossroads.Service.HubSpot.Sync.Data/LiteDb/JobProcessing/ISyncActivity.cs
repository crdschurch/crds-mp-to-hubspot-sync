using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface ISyncActivity : IPersist<string>, ISyncResult
    {
        SyncDates PreviousSyncDates { get; set; }

        int EmailAddressAlreadyExistsCount { get; }

        int EmailAddressDoesNotExistCount { get; }

        int HubSpotApiRequestCount { get; }

        ISyncActivityOperation NewRegistrationOperation { get; set; }

        ISyncActivityOperation CoreUpdateOperation { get; set; }

        ISyncActivityChildAgeAndGradeUpdateOperation ChildAgeAndGradeUpdateOperation { get; set; }

        SyncProgress SyncProgress { get; set; }
    }
}
