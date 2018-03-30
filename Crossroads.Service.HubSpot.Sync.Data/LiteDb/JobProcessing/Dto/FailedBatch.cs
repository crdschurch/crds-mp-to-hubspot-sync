using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class FailedBatch
    {
        public int Count { get; set; }

        public string HttpStatusCode { get; set; }

        public string Reason { get; set; }

        public int BatchNumber { get; set; }

        public HubSpotContact[] Contacts { get; set; }
    }
}