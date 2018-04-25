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
        BulkSyncResult BulkCreateOrUpdate(BulkContact[] contacts);

        /// <summary>
        /// After retrying in bulk, if not all contacts have been synced, let's try again one at a time.
        /// </summary>
        /// <param name="contacts">List of contacts to create serially.</param>
        SerialCreateSyncResult<TCreateContact> SerialCreate<TCreateContact>(TCreateContact[] contacts) where TCreateContact : IContact;

        /// <summary>
        /// Try updating HubSpot with the latest contact data changes.
        /// </summary>
        /// <param name="contacts">List of contacts to update serially.</param>
        CoreUpdateResult<TUpdateContact> SerialUpdate<TUpdateContact>(TUpdateContact[] contacts) where TUpdateContact : IUpdateContact;
    }
}
