
namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    public class NewlyRegisteredContactDto
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
        /// [HubSpot Definition]: How ready a contact might be for a sale. This can be tied to imports, forms, workflows, or manually by contact.
        /// </summary>
        public string LifeCycleStage => "customer";

        public string Source => "MP_Registration";
    }
}
