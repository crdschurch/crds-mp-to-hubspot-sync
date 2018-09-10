using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public struct OperationDates
    {
        public DateTime RegistrationSyncDate { get; set; }

        public DateTime CoreUpdateSyncDate { get; set; }

        /// <summary>
        /// The date and time we run the Ministry Platform age and grade group
        /// delta process.
        /// </summary>
        public DateTime AgeAndGradeProcessDate { get; set; }

        /// <summary>
        /// The date and time we sync the MP age and grade group data to HubSpot.
        /// </summary>
        public DateTime AgeAndGradeSyncDate { get; set; }
    }
}
