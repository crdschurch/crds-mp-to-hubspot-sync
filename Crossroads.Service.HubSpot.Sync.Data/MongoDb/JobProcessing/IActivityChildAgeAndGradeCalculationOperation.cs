using System;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing
{
    public interface IActivityChildAgeAndGradeCalculationOperation
    {
        ExecutionTime Execution { get; }

        /// <summary>
        /// The last time the calculation process was completed successfully.
        /// </summary>
        DateTime PreviousProcessDate { get; set; }

        DateTime PreviousSyncDate { get; set; }

        ChildAgeAndGradeDeltaLogDto AgeGradeDeltaLog { get; set; }
    }
}
