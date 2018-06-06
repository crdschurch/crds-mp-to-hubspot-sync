namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    /// <summary>
    /// <see cref="ICoreContactProperties"/> PROPERTY NAMES MUST MATCH THE COLUMNS NAMES IN
    /// dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot and
    /// dbo.api_crds_get_mp_contact_updates_for_hubspot. When this is true, the mapping from this
    /// instance to a HubSpot-bound DTO will happen automagically (thanks to a smidge of reflection).
    /// </summary>
    public interface ICoreContactProperties
    {
        /// <summary>
        /// Could be present in the individual property, **IF** someone updated their email address.
        /// </summary>
        string Email { get; set; }

        string Firstname { get; set; }

        string Lastname { get; set; }

        string Marital_Status { get; set; }

        string Gender { get; set; }

        /// <summary>
        /// The congregation/site of a contact's household.
        /// </summary>
        string Community { get; set; }

        /// <summary>
        /// Home phone number
        /// </summary>
        string Phone { get; set; }

        string MobilePhone { get; set; }

        string Zip { get; set; }
    }
}