
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    /// <summary>
    /// Service class for creating new Ministry Platform contacts to HubSpot.
    /// </summary>
    public interface ISyncNewMpRegistrationsToHubSpot
    {
        /// <summary>
        /// Fire off!!!!
        /// </summary>
        IActivityResult Execute();
    }
}