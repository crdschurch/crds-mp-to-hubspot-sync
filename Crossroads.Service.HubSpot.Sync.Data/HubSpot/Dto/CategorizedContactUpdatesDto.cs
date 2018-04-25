using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Dto
{
    public class CategorizedContactUpdatesDto
    {
        /// <summary>
        /// List of contacts whose email address changed from one value to another.
        /// </summary>
        public List<EmailAddressChangedContact> EmailChangedContacts { get; set; }

        /// <summary>
        /// List of contacts with non-email changes.
        /// </summary>
        public List<CoreOnlyChangedContact> CoreOnlyChangedContacts { get; set; }
    }
}
