using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct SyncDates
    {
        public DateTime RegistrationSyncDate { get; set; }

        public DateTime CoreUpdateSyncDate { get; set; }
    }
}
