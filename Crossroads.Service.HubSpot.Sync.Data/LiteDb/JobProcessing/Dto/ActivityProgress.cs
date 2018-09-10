using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class ActivityProgress
    {
        /// <summary>
        /// Commenting mostly for visibility into the fact that this constructor is a bit busier than its contemporaries.
        /// Newing this object up primes the Steps property with all SyncStepName values and a <see cref="OperationState"/>
        /// of "<see cref="OperationState.Pending"/>".
        /// </summary>
        public ActivityProgress()
        {
            Operations = new Dictionary<OperationName, OperationDetail>();
            PrimeOperations();
        }

        public ActivityState ActivityState { get; set; }

        public string Duration { get; set; }

        public Dictionary<OperationName, OperationDetail> Operations { get; set; }

        private void PrimeOperations()
        {
            foreach (var syncStepName in System.Enum.GetValues(typeof(OperationName)).Cast<OperationName>())
            {
                Operations.Add(syncStepName, new OperationDetail {OperationState = OperationState.Pending});
            }
        }

        public string HtmlPrint()
        {
            return $@"Activity State: <strong>{ActivityState}</strong><br/>
            Duration: {Duration}<br/><br/>

            {string.Join("<br/><br/>", Operations.Select(k => $"<u>{MakeEnumStringHumanReadable(k.Key.ToString())}</u><br/>{k.Value.HtmlPrint()}"))}";
        }

        public string MakeEnumStringHumanReadable(string enumStringWithCapitalLetters)
        {
            return Regex.Replace(enumStringWithCapitalLetters, "([a-z])([A-Z])", "$1 $2");
        }
    }
}