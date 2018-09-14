using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto
{
    public class OperationDetail
    {
        public OperationState OperationState { get; set; }

        public int ContactCount { get; set; }

        public string Duration { get; set; }

        internal string PlainTextPrint()
        {
            return $@"Operation State: {OperationState}
Contact count: {ContactCount}
Duration: {Duration}";
        }

        internal string HtmlPrint()
        {
            return $"Operation State: <strong>{OperationState}</strong><br/>Contact count: {ContactCount}<br/>Duration: {Duration}";
        }
    }
}