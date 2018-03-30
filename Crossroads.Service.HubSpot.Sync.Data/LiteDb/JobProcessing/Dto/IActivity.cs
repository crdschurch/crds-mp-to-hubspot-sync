using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface IActivity
    {
        DateTime ActivityDateTime { get; set; }
        List<FailedBatch> FailedBatches { get; set; }
        int FailureCount { get; set; }
        string Id { get; }
        int SuccessCount { get; set; }
        int TotalContacts { get; set; }
    }
}