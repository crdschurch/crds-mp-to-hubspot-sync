
namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    /// <summary>
    /// Pivoted/vertical representation of changed fields. There will be as many
    /// of these objects for a given contacts as there are fields that have changed.
    /// We knit these contact field changes together based on the value of the
    /// <see cref="MinistryPlatformContactId"/> property.
    /// 
    /// Includes core, Crossroads-required* fields as well, JUST IN CASE
    /// this contact does not yet exist in HubSpot (for whatever reason).
    /// Required fields: First name, Last name, Email, Community (congregation).
    /// 
    /// <see cref="ICoreContactProperties"/> PROPERTY NAMES MUST MATCH THE COLUMNS NAMES IN
    /// dbo.api_crds_get_mp_contact_updates_for_hubspot.
    /// </summary>
    public class CoreUpdateMpContactDto : IDeveloperIntegrationProperties, ICoreContactProperties
    {
        public string MinistryPlatformContactId { get; set; }

        public string PropertyName { get; set; }

        public string PreviousValue { get; set; }

        public string NewValue { get; set; }

        /// <summary>
        /// Could be present in the individual property, **IF** someone updated their email address.
        /// </summary>
        public string Email { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string Marital_Status { get; set; }

        public string Gender { get; set; }

        /// <summary>
        /// The congregation/site of a contact's household.
        /// </summary>
        public string Community { get; set; }

        public string Phone { get; set; }

        public string MobilePhone { get; set; }

        public string Zip { get; set; }

        public string Source => "MP_Sync_General_Update";
    }
}
