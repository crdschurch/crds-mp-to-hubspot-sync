using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDb;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface ISyncActivity : IPersist<string>
    {
        Guid SyncActivityId { get; }

        IExecutionTime Execution { get; }

        SyncDates PreviousSyncDates { get; set; }

        int TotalContacts { get; }

        int SuccessCount { get; }

        int EmailAddressAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        ISyncActivityNewRegistrationOperation NewRegistrationOperation { get; set; }

        ISyncActivityCoreUpdateOperation CoreUpdateOperation { get; set; }

        ISyncActivityChildAgeAndGradeUpdateOperation ChildAgeAndGradeUpdateOperation { get; set; }

        SyncProcessingState SyncProcessingState { get; set; }
    }
}
