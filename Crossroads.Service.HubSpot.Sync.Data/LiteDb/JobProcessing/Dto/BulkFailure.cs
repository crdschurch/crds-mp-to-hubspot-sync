using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class BulkFailure
    {
        public int Count { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string Reason { get; set; }

        public int BatchNumber { get; set; }

        public BulkContact[] Contacts { get; set; }
    }
}
