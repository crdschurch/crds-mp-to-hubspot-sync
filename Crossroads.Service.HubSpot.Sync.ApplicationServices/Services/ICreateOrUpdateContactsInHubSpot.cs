using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    public interface ICreateOrUpdateContactsInHubSpot
    {
        /// <summary>
        /// Creates and/or updates HubSpot contacts in bulk.
        /// </summary>
        /// <param name="hubSpotContacts">List of Ministry Platform contacts to sync to HubSpot.</param>
        /// <param name="batchSize">Number of contacts to send to HubSpot per request.</param>
        BulkSyncResult BulkSync(BulkHubSpotContact[] hubSpotContacts, int batchSize = 100);

        /// <summary>
        /// Creates contacts serially.
        /// </summary>
        SerialSyncResult SerialCreate(SerialHubSpotContact[] hubSpotContacts);

        /// <summary>
        /// Updates contacts serially. Can also update email addresses.
        /// </summary>
        SerialSyncResult SerialUpdate(SerialHubSpotContact[] hubSpotContacts);

        /// <summary>
        /// Responsible for deleting the contact record of the old email address that is not able to be updated
        /// to the "new" email address due to the fact that the email address we wish to switch to already exists.
        /// We're ok deleting the existing account b/c Ministry Platform's dp_Users.User_Name field is our source
        /// of truth, is not nullable and has a unique constraint; so the contact attempting to update to a given
        /// email address is the true owner of the account.
        /// 
        /// 1) Get contact by old email address
        /// 2) Delete contact by VID (acquired by old email address)
        /// 3) Update contact in HubSpot with new email address in both the url and the post body
        /// </summary>
        SerialSyncResult ReconcileConflicts(SerialHubSpotContact[] hubSpotContacts);
    }
}
