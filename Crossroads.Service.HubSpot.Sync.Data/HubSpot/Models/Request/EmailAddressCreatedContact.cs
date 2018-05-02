
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    /// <summary>
    /// Represents the scenario when a contact previously existed but had no email
    /// address associated.
    /// 
    /// In this scenario, we will try to create them, operating under the assumption
    /// they did not previously exist in HubSpot b/c they had no email address. If
    /// we receive an HTTP status code of 409 (Conflict), no harm done b/c they already
    /// exist.
    /// 
    /// If a 409 occurs, we'll attempt to make a core-only update.
    /// </summary>
    public class EmailAddressCreatedContact : SerialCreateContact
    {
        /// <summary>
        /// Backup plan in the event the contact already exists in HubSpot.
        /// </summary>
        [JsonIgnore]
        public CoreOnlyChangedContact ContactAlreadyExistsContingency { get; set; }
    }
}
