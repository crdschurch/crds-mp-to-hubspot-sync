using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class SyncProgress
    {
        /// <summary>
        /// Commenting mostly for visibility into the fact that this constructor is a bit busier than its contemporaries.
        /// Newing this object up primes the Steps property with all SyncStepName values and a <see cref="SyncStepState"/>
        /// of "<see cref="SyncStepState.Pending"/>".
        /// </summary>
        public SyncProgress()
        {
            Steps = new Dictionary<SyncStepName, SyncStep>();
            PrimeSteps();
        }

        public SyncState SyncState { get; set; }

        public Dictionary<SyncStepName, SyncStep> Steps { get; set; }

        private void PrimeSteps()
        {
            foreach (var syncStepName in System.Enum.GetValues(typeof(SyncStepName)).Cast<SyncStepName>())
            {
                Steps.Add(syncStepName, new SyncStep {StepState = SyncStepState.Pending});
            }
        }
    }
}