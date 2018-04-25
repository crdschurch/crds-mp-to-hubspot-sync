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

        int ContactAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        ISyncActivityNewRegistrationOperation NewRegistrationOperation { get; set; }

        ISyncActivityCoreUpdateOperation CoreUpdateOperation { get; set; }

        SyncProcessingState SyncProcessingState { get; set; }

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save a last successful date for
        /// new registrations b/c records have been rejected that need to be retried.
        /// </summary>
        bool HubSpotIssuesWereEncounteredDuringNewRegistrationOperation();

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save last successful dates for core
        /// contact updates b/c records have been rejected that need to be retried.
        /// </summary>
        bool HubSpotIssuesWereEncounteredDuringCoreUpdateOperation();
    }
}
