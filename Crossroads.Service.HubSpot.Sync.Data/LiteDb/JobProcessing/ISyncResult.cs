namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    public interface ISyncResult
    {
        IExecutionTime Execution { get; }
        int FailureCount { get; }
        int SuccessCount { get; }
        int TotalContacts { get; }
    }
}