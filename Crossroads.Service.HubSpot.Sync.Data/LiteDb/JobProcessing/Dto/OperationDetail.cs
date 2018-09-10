using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto
{
    public class OperationDetail
    {
        public OperationState OperationState { get; set; }

        public int ContactCount { get; set; }

        public string Duration { get; set; }

        public string HtmlPrint()
        {
            return $"Operation State: <strong>{OperationState}</strong><br/>Contact count: {ContactCount}<br/>Duration: {Duration}";
        }
    }
}