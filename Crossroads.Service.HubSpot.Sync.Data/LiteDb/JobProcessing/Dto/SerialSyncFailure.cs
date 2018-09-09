using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialSyncFailure : IFailureDetails
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public HubSpotException Exception { get; set; }

        public IHubSpotContact HubSpotContact { get; set; }
    }
}