
namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    public class NewlyRegisteredMpContactDto
    {
        public string MinistryPlatformContactId { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string Email { get; set; }

        /// <summary>
        /// The congregation/site of a contact.
        /// </summary>
        public string Community { get; set; }

        /// <summary>
        /// Will contain one of the following values as of its inclusion here:
        /// Single
        /// Married
        /// Divorced
        /// Widowed
        /// Separated
        /// 
        /// or... an empty string.
        /// 
        /// On the HubSpot side of the house we've added an
        /// entry to the marital_status property of 'Unspecified' with an empty
        /// string value. This accommodates when MP's dbo.Contacts.Marital_Status_ID
        /// is NULL or when the audit log indicates what was previously populated is
        /// now NULL. In the queries that feed this and other objects, we return an
        /// empty string instead of NULL.
        /// 
        /// This is likely a pattern we'll adopt for any HubSpot properties created
        /// as lists containing finite values. 'Unspecified' is NOT visible to
        /// consumers of the Marital Status field (on a form, etc).
        /// </summary>
        public string MaritalStatus { get; set; }

        /// <summary>
        /// [HubSpot Definition]: How ready a contact might be for a sale. This can be tied to imports, forms, workflows, or manually by contact.
        /// </summary>
        public string LifeCycleStage => "customer";

        public string Source => "MP_Registration";
    }
}
