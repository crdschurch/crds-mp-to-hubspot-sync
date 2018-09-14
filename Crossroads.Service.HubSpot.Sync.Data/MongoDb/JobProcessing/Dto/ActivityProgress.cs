using System;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class ActivityProgress
    {
        private static readonly string Crlf = Environment.NewLine;

        /// <summary>
        /// Commenting mostly for visibility into the fact that this constructor is a bit busier than its contemporaries.
        /// Newing this object up primes the Steps property with all SyncStepName values and a <see cref="OperationState"/>
        /// of "<see cref="OperationState.Pending"/>".
        /// </summary>
        public ActivityProgress()
        {
            Operations = new Dictionary<string, OperationDetail>();
            PrimeOperations();
        }

        public ActivityState ActivityState { get; set; }

        public string Duration { get; set; }

        public Dictionary<string, OperationDetail> Operations { get; set; }

        private void PrimeOperations()
        {
            foreach (var syncStepName in System.Enum.GetValues(typeof(OperationName)).Cast<OperationName>())
            {
                Operations.Add(syncStepName.ToString(), new OperationDetail {OperationState = OperationState.Pending});
            }
        }

        public string PlainTextPrint()
        {
            return $@"
Activity State: {ActivityState}
Duration: {Duration}

{string.Join($"{Crlf}{Crlf}", Operations.Select(k => $"{MakeEnumStringHumanReadable(k.Key)}{Crlf}{k.Value.PlainTextPrint()}"))}";
        }

        public string HtmlPrint()
        {
            return $@"Activity State: <strong>{ActivityState}</strong><br/>
            Duration: {Duration}<br/><br/>

            {string.Join("<br/><br/>", Operations.Select(k => $"<u>{MakeEnumStringHumanReadable(k.Key)}</u><br/>{k.Value.HtmlPrint()}"))}";
        }

        public string MakeEnumStringHumanReadable(string wordsCrammedTogetherToBeSeparatedByTitleCase)
        {
            return Regex.Replace(wordsCrammedTogetherToBeSeparatedByTitleCase, "([a-z])([A-Z])", "$1 $2");
        }
    }
}