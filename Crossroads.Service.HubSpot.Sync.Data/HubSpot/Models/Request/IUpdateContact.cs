﻿using Newtonsoft.Json;

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

        /// <summary>
        /// Contingency for when the contact we've attempted to update does NOT yet exist in
        /// HubSpot. Will accommodate the scenario when a contact's invalid email address is
        /// updated to one HubSpot considers valid and finally accepts/creates the contact.
        /// </summary>
        [JsonIgnore]
        EmailAddressCreatedContact ContactDoesNotExistContingency { get; set; }

        /// <summary>
        /// Contingency for when the email address of the contact we've attempted to update
        /// ALREADY exists in HubSpot. Will accommodate the scenario by retrying with core
        /// only updates for the updated email address. We are not fearful of overwriting
        /// another contact's information, b/c a given email address can belong to only ONE
        /// MP user (dbo.dp_Users.User_Name is unique, according to tribal knowledge)
        /// </summary>
        [JsonIgnore]
        CoreOnlyChangedContact ContactAlreadyExistsContingency { get; set; }
    }
}