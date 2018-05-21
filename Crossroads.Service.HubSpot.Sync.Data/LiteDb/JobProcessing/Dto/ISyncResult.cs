namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public interface ISyncResult
    {
        IExecutionTime Execution { get; }
        int FailureCount { get; }
        int SuccessCount { get; }
        int TotalContacts { get; }
    }
}