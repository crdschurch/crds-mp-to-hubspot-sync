using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.MP
{
    public interface IMinistryPlatformContactRepository
    {
        /// <summary>
        /// Gets all contacts that were registered since the last time we synced
        /// MP contacts to HubSpot.
        /// </summary>
        /// <param name="lastSuccessfulSyncDateUtc">
        /// The date from which to check for new contacts.
        /// </param>
        IList<NewlyRegisteredMpContactDto> GetNewlyRegisteredContacts(DateTime lastSuccessfulSyncDateUtc);

        /// <summary>
        /// Gets all contact updates since the last time we synced MP contacts to HubSpot.
        /// </summary>
        /// <param name="lastSuccessfulSyncDateUtc">
        /// The date from which to check for contact updates.
        /// </param>
        IDictionary<string, List<MpContactUpdateDto>> GetContactUpdates(DateTime lastSuccessfulSyncDateUtc);
    }
}
