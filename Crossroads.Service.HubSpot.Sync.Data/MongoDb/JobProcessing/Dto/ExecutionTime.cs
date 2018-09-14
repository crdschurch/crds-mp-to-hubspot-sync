using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class ExecutionTime : IExecutionTime
    {
        public ExecutionTime() { }

        public ExecutionTime(DateTime start)
        {
            StartUtc = FinishUtc = start;
        }

        public DateTime StartUtc { get; set; }

        public DateTime FinishUtc { get; set; }

        public string Duration => (FinishUtc - StartUtc).ToString("g");
    }
}