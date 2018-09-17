namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum
{
    public enum OperationState
    {
        /// <summary>
        /// Default state.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Processing is in flight.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Processing has completed.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Processing has completed but the validation step failed.
        /// </summary>
        CompletedButWithIssues = 3,

        /// <summary>
        /// Processing aborted (due to exception).
        /// </summary>
        Aborted = 4
    }
}