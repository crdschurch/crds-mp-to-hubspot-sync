using System;

namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    /// <summary>
    /// Number of updates and inserts to the Kids Club and Student Ministry Age and Grade data set:
    /// dbo.cr_ChildAgeAndGradeCountsByHousehold in Ministry Platform.
    /// </summary>
    public class ChildAgeAndGradeDeltaLogDto
    {
        public DateTime ProcessedUtc { get; set; }

        public DateTime? SyncCompletedUtc { get; set; }

        public int InsertCount { get; set; }

        public int UpdateCount { get; set; }
    }
}