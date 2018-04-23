using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    /// <summary>
    /// Carries updated information about a contact from MP into HubSpot.
    /// In the event the contact does not yet exist, this interface serves
    /// to preserve core information about the contact for the sake of
    /// creation.
    /// </summary>
    public interface IUpdateContact : IContact
    {
        [JsonIgnore]
        string Email { get; set; }

        [JsonIgnore]
        string Firstname { get; set; }

        [JsonIgnore]
        string Lastname { get; set; }

        [JsonIgnore]
        string MaritalStatus { get; set; }

        /// <summary>
        /// Contingency for when the contact we've attempted to update does NOT yet exist in
        /// HubSpot. Will accommodate the scenario when a contact's invalid email address is
        /// updated to one HubSpot considers valid and finally accepts/creates the contact.
        /// </summary>
        [JsonIgnore]
        EmailAddressCreatedContact ContactDoesNotExistContingency { get; set; }
    }
}