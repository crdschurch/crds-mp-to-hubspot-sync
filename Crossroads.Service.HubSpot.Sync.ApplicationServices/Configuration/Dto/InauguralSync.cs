
using System;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto
{
    /// <summary>
    /// Represents the first time we'll sync both newly registered and pre-existing Ministry
    /// Platform CRM contacts to our HubSpot CRM instance (for the sake of marketing).
    /// </summary>
    public class InauguralSync
    {
        /// <summary>
        /// The date from which we should start looking for new Ministry Platform contact
        /// registrations.
        /// </summary>
        public DateTime RegistrationSyncDate { get; set; }

        /// <summary>
        /// The date from which we should start looking for Ministry Platform core (first
        /// name, last name, email, community, marital status) contact updates.
        /// </summary>
        public DateTime CoreUpdateSyncDate { get; set; }
    }
}
