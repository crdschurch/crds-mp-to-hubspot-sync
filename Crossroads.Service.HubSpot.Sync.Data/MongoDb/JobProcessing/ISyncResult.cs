using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing
{
    public interface ISyncResult
    {
        ExecutionTime Execution { get; }
        int FailureCount { get; }
        int SuccessCount { get; }
        int TotalContacts { get; }
    }
}