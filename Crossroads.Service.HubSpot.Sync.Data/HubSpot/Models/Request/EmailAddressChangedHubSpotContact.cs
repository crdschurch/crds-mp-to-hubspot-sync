namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    /// <summary>
    /// Represents the scenario when an existing contact changed their email address.
    /// The email address is the unique identifier in HubSpot, so it's important that we
    /// make this distinction in our app code and handle it differently from other contact
    /// attribute/property changes.
    /// </summary>
    public class EmailAddressChangedHubSpotContact : SerialHubSpotContact
    {
    }
}