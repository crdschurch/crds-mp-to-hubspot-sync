using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncActivity : ISyncActivity
    {
        public SyncActivity() { }

        public SyncActivity(Guid syncActivityId, DateTime executionStartTime)
        {
            SyncActivityId = syncActivityId;
            Execution = new ExecutionTime(executionStartTime);
            Id = $"{nameof(SyncActivity)}_{Execution.StartUtc:u}"; // ISO8601: universal/sortable
        }

        [BsonField("_id")]
        public string Id { get; set; }

        public DateTime LastUpdated { get; set; }

        public Guid SyncActivityId { get; set; }

        public IExecutionTime Execution { get; set; }

        public SyncDates PreviousSyncDates { get; set; }

        public int TotalContacts => NewRegistrationOperation?.TotalContacts ?? 0 +
                                    CoreUpdateOperation?.TotalContacts ?? 0;

        public ISyncActivityNewRegistrationOperation NewRegistrationOperation { get; set; }

        public ISyncActivityCoreUpdateOperation CoreUpdateOperation { get; set; }

        public SyncProcessingState SyncProcessingState { get; set; }
    }
}