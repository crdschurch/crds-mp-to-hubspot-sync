using System;
using System.Collections.Generic;
using System.Text;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct SyncDates
    {
        public DateTime CreateSyncDate { get; set; }

        public DateTime UpdateSyncDate { get; set; }
    }
}
