using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response
{
    public class HubSpotException
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty(PropertyName = "validationResults")]
        public ValidationResult[] ValidationResults { get; set; }

        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }
    }

    public class ValidationResult
    {
        [JsonProperty(PropertyName = "isValid")]
        public bool IsValid { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}