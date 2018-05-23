using System;
using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.MP
{
    public interface IMinistryPlatformContactRepository
    {
        /// <summary>
        /// Fetches, segments and persists the number of children (ostensibly from ages 0 - 18) in a given household by 20
        /// age/grade groups as defined by both Student and Kids Club ministries. The purpose of this data is ultimately
        /// to determine which HubSpot-persisted contacts will receive Kids Club and/or Student Ministry email notifications
        /// while children in their same household remain in this age range.
        /// </summary>
        ChildAgeAndGradeDeltaLogDto CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas();

        /// <summary>
        /// Gets all contacts that were registered since the last time we synced
        /// MP contacts to HubSpot.
        /// </summary>
        /// <param name="lastSuccessfulSyncDateUtc">
        /// The date from which to check for new contacts.
        /// </param>
        IList<NewlyRegisteredMpContactDto> GetNewlyRegisteredContacts(DateTime lastSuccessfulSyncDateUtc);

        /// <summary>
        /// Gets all contact updates since the last time we synced MP contacts to HubSpot.
        /// </summary>
        /// <param name="lastSuccessfulSyncDateUtc">
        /// The date from which to check for contact updates.
        /// </param>
        IDictionary<string, List<CoreUpdateMpContactDto>> GetAuditedContactUpdates(DateTime lastSuccessfulSyncDateUtc);

        /// <summary>
        /// Gets all of the (pre-calculated) changes to the Kids Club and Student Ministry age and grade counts stored
        /// by HouseholdId and joins them to all registered contacts within a given household so these counts of children
        /// per age and grade group within the household will be updated in HubSpot, which will drive which contacts receive
        /// the Kids Club and Student Ministry email notifications from HubSpot.
        /// </summary>
        IList<AgeAndGradeGroupCountsForMpContactDto> GetAgeAndGradeGroupDataForContacts();

        /// <summary>
        /// Sets the age &amp; grade sync completed date in dbo.cr_ChildAgeAndGradeDeltaLog to signify the age/grade counts
        /// have been successfully synced to HubSpot.
        /// </summary>
        DateTime SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate();
    }
}