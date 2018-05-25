
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
    public class CoreUpdateMpContactDto : IDeveloperIntegrationProperties, ICoreContactProperties, IAgeGradeContactProperties
    {
        public string PropertyName { get; set; }

        public string PreviousValue { get; set; }

        public string NewValue { get; set; }

        public string MinistryPlatformContactId { get; set; }

        public string Source => "MP_Sync_General_Update";

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

        public int Number_of_Infants { get; set; }

        public int Number_of_1_Year_Olds { get; set; }

        public int Number_of_2_Year_Olds { get; set; }

        public int Number_of_3_Year_Olds { get; set; }

        public int Number_of_4_Year_Olds { get; set; }

        public int Number_of_5_Year_Olds { get; set; }

        public int Number_of_Kindergartners { get; set; }

        public int Number_of_1st_Graders { get; set; }

        public int Number_of_2nd_Graders { get; set; }

        public int Number_of_3rd_Graders { get; set; }

        public int Number_of_4th_Graders { get; set; }

        public int Number_of_5th_Graders { get; set; }

        public int Number_of_6th_Graders { get; set; }

        public int Number_of_7th_Graders { get; set; }

        public int Number_of_8th_Graders { get; set; }

        public int Number_of_9th_Graders { get; set; }

        public int Number_of_10th_Graders { get; set; }

        public int Number_of_11th_Graders { get; set; }

        public int Number_of_12th_Graders { get; set; }

        public int Number_of_Graduated_Seniors { get; set; }
    }
}
