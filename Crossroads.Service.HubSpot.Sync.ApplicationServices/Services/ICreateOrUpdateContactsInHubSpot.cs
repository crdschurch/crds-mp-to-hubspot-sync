
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    public interface ICreateOrUpdateContactsInHubSpot
    {
        TActivity BulkCreateOrUpdate<TActivity>(HubSpotContact[] contactsToPushUpToHubSpot, TActivity activity)
            where TActivity : IActivity;

        NewContactActivity RetryBulkCreate(NewContactActivity firstPass);

        NewContactActivity Create(NewContactActivity thirdPass);
    }
}
