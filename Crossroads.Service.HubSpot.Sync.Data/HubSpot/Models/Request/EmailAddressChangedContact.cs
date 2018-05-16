namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    /// <summary>
    /// Represents the scenario when an existing contact changed their email address.
    /// 
    /// Should we check for their existence first? I think so. We'll pull back the
    /// Ministry Platform contact id and compare it to the one we're considering
    /// updating with beforehand, so as not to overwrite a MP Head of Household's
    /// HubSpot account with a spouse's or a child's. If there is no
    /// MinistryPlatformContactId, we'll update and assign it.
    /// </summary>
    public class EmailAddressChangedContact : SerialContact
    {
    }
}