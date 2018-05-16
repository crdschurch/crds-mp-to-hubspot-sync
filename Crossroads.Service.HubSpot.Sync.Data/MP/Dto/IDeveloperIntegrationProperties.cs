namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    /// <summary>
    /// Values to be passed over during the integration process that are kind of
    /// tangential but add context to the contact's origin.
    /// </summary>
    public interface IDeveloperIntegrationProperties
    {
        /// <summary>
        /// Ministry Platform contact unique identifier.
        /// </summary>
        string MinistryPlatformContactId { get; set; }

        /// <summary>
        /// Developer designation for the origin of a record's most recent update.
        /// </summary>
        string Source { get; }
    }
}
