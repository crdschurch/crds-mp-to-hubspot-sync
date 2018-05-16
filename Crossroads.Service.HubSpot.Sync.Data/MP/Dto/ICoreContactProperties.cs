namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    public interface ICoreContactProperties
    {
        /// <summary>
        /// Could be present in the individual property, **IF** someone updated their email address.
        /// </summary>
        string Email { get; set; }

        string Firstname { get; set; }

        string Lastname { get; set; }

        string MaritalStatus { get; set; }

        string Gender { get; set; }

        /// <summary>
        /// The congregation/site of a contact's household.
        /// </summary>
        string Community { get; set; }
    }
}