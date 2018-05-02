using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class CoreUpdateFailure<TUpdateContact> : IFailureDetails where TUpdateContact : IUpdateContact
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public HubSpotException Exception { get; set; }

        public TUpdateContact Contact { get; set; }
    }
}