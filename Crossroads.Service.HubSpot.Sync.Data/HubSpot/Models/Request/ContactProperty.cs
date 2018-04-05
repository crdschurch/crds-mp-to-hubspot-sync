using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class ContactProperty
    {
        [JsonProperty(PropertyName = "property")]
        public string Property { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}