using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class ExecutionTime : IExecutionTime
    {
        public ExecutionTime(DateTime start)
        {
            StartUtc = FinishUtc = start;
        }

        public DateTime StartUtc { get; }

        public DateTime FinishUtc { get; set; }

        public string Duration => (FinishUtc - StartUtc).ToString("g");
    }
}