using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;
using System.Net;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IFailureDetails
    {
        HttpStatusCode HttpStatusCode { get; set; }

        HubSpotException Exception { get; set; }
    }
}
