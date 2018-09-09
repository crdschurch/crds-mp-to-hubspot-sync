using System;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface IExecutionTime
    {
        DateTime StartUtc { get; }

        DateTime FinishUtc { get; set; }

        string Duration { get; }
    }
}