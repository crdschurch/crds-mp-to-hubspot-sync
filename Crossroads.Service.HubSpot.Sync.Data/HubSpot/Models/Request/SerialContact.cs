using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class SerialContact : IContact
    {
        [JsonIgnore]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public List<ContactProperty> Properties { get; set; }
    }
}