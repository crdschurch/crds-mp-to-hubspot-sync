using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    /// <summary>
    /// Utility for cleaning up activity data so that it is much lighter when we commit them to the embedded db.
    /// Some operational processing data is critical when in flight, but noisy post-processing.
    /// </summary>
    public interface ICleanUpActivity
    {
        /// <summary>
        /// Cleans up activity data.
        /// </summary>
        /// <param name="activity">The entity to clean up.</param>
        void CleanUp(IActivity activity);
    }
}
