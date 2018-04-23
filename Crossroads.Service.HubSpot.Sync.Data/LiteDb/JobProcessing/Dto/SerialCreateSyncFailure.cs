using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SerialCreateSyncFailure<TContact> where TContact : IContact
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string Reason { get; set; }

        public TContact Contact { get; set; }
    }
}