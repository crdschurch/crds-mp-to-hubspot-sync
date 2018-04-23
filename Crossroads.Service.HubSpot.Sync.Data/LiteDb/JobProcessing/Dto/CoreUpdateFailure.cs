using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class CoreUpdateFailure<TUpdateContact> where TUpdateContact : IUpdateContact
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string Reason { get; set; }

        public TUpdateContact Contact { get; set; }
    }
}