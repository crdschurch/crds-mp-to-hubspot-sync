using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    public interface ICreateOrUpdateContactsInHubSpot
    {
        /// <summary>
        /// Creates and/or updates HubSpot contacts in bulk.
        /// </summary>
        /// <param name="contacts">List of Ministry Platform contacts to sync to HubSpot.</param>
        /// <param name="batchSize">Number of contacts to send to HubSpot per request.</param>
        BulkSyncResult BulkSync(BulkContact[] contacts, int batchSize = 100);

        /// <summary>
        /// Creates or updates contacts serially. Can also update email addresses.
        /// </summary>
        SerialSyncResult SerialSync(SerialContact[] contacts);
    }
}
