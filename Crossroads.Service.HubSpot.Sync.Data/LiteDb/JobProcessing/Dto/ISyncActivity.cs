using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface ISyncActivity : IPersist<string>, ISyncResult
    {
        SyncDates PreviousSyncDates { get; set; }

        int EmailAddressAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        ISyncActivityNewRegistrationOperation NewRegistrationOperation { get; set; }

        ISyncActivityCoreUpdateOperation CoreUpdateOperation { get; set; }

        ISyncActivityChildAgeAndGradeUpdateOperation ChildAgeAndGradeUpdateOperation { get; set; }

        SyncProcessingState SyncProcessingState { get; set; }
    }
}
