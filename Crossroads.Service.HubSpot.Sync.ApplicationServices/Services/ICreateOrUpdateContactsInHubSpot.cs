
using System.Threading.Tasks;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    public interface ICreateOrUpdateContactsInHubSpot
    {
        Task<JobActivityDto> CreateOrUpdateAsync(HubSpotContact[] contactsToPushUpToHubSpot);
    }
}
