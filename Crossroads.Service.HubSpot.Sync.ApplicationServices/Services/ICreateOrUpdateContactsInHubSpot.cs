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
        BulkActivityResult BulkCreateOrUpdate(BulkContact[] contacts);

        /// <summary>
        /// After retrying in bulk, if not all contacts have been synced, let's try again one at a time.
        /// </summary>
        /// <param name="contacts">List of contacts to create serially.</param>
        SerialActivityResult SerialCreate(SerialContact[] contacts);
    }
}
