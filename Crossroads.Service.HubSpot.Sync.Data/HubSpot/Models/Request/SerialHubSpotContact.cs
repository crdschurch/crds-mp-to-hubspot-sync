using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class SerialHubSpotContact : IHubSpotContact
    {
        [JsonIgnore]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public List<HubSpotContactProperty> Properties { get; set; }
    }
}