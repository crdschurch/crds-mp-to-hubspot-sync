using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    /// <summary>
    /// Breaks the most recent contact updates into the following categories:
    /// 1) Contacts who changed their email address
    /// 2) Other changes
    /// 
    /// Isolating email-related changes from other changes b/c on HubSpot the
    /// email address is the unique identifier. When 1) is true, we're
    /// attempting to change a contact's (already existent in HubSpot) email
    /// address. 2) represents all other changes we hope to capture, which are:
    /// 
    /// 1) First name
    /// 2) Last name
    /// 3) Community
    /// 4) Marital status
    /// 5) Gender
    /// 6) Home Phone
    /// 7) Mobile Phone
    /// 8) Postal/Zip Code
    /// </summary>
    public interface IPrepareMpDataForHubSpot
    {
        SerialHubSpotContact[] Prep(IList<NewlyRegisteredMpContactDto> newContacts);

        SerialHubSpotContact[] Prep(IDictionary<string, List<CoreUpdateMpContactDto>> contactUpdates);

        BulkHubSpotContact[] Prep(IList<AgeAndGradeGroupCountsForMpContactDto> mpContacts);

        BulkHubSpotContact[] ToBulk(List<BulkSyncFailure> failedBatches);

        SerialHubSpotContact[] ToSerial(List<BulkSyncFailure> failedBatches);
    }
}