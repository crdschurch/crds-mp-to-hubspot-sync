using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialCreateSyncFailure<TContact> : IFailureDetails where TContact : IContact
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public HubSpotException Exception { get; set; }

        public TContact Contact { get; set; }
    }
}