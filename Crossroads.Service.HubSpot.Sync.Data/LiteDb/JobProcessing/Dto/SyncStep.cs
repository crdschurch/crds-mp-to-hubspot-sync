using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncStep
    {
        public SyncStepState StepState { get; set; }

        public int NumberOfContactsToSync { get; set; }
    }
}