using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class BulkSyncFailure : IFailureDetails
    {
        public int Count { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public HubSpotException Exception { get; set; }

        public int BatchNumber { get; set; }

        public BulkContact[] Contacts { get; set; }
    }
}
