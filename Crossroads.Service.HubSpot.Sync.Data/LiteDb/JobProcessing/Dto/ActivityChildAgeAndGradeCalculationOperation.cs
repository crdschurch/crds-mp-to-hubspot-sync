using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class ActivityChildAgeAndGradeCalculationOperation : IActivityChildAgeAndGradeCalculationOperation
    {
        public ActivityChildAgeAndGradeCalculationOperation(DateTime executionStartTime)
        {
            Execution = new ExecutionTime(executionStartTime);
            AgeGradeDeltaLog = new ChildAgeAndGradeDeltaLogDto();
        }

        public ActivityChildAgeAndGradeCalculationOperation()
        {
            Execution = new ExecutionTime();
            AgeGradeDeltaLog = new ChildAgeAndGradeDeltaLogDto();
        }

        public IExecutionTime Execution { get; }

        public DateTime PreviousProcessDate { get; set; }

        public DateTime PreviousSyncDate { get; set; }

        public ChildAgeAndGradeDeltaLogDto AgeGradeDeltaLog { get; set; }
    }
}