using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    /// <summary>
    /// Breaks the most recent contact updates into the following categories:
    /// 1) Contacts who previously had no email address and now have one
    /// 2) Contacts who changed their email address
    /// 3) Other changes
    /// 
    /// Isolating email-related changes from other changes b/c on HubSpot the
    /// email address is the unique identifier. So when 1) is true, we're
    /// attempting to create a contact in HubSpot. When 2) is true, we're
    /// attempting to change a contact's (already existent in HubSpot) email
    /// address. 3) represents all other changes we hope to capture, which are:
    /// 
    /// 1) First name
    /// 2) Last name
    /// 3) Community
    /// 4) Marital status
    /// </summary>
    public interface IPrepareMpContactCoreUpdatesForHubSpot
    {
        CategorizedContactUpdatesDto Prepare(IDictionary<string, List<MpContactUpdateDto>> contactUpdates);
    }
}
