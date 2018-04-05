using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response
{
    public class Conflict
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        //[JsonProperty("identityProfile")]
        //public Identityprofile IdentityProfile { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
    }

    public class Identityprofile
    {
        public int vid { get; set; }
        public Identity[] identity { get; set; }
        public object[] linkedVid { get; set; }
        public bool isContact { get; set; }
        public long savedAtTimestamp { get; set; }
    }

    public class Identity
    {
        public string value { get; set; }
        public string type { get; set; }
        public long timestamp { get; set; }
        public bool isPrimary { get; set; }
    }

}
