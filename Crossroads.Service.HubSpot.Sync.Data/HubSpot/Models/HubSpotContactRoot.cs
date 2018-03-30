using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models
{
    public class HubSpotContactRoot
    {
        public HubSpotContact[] Contacts { get; set; }
    }

    public class HubSpotContact
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public List<ContactProperty> Properties { get; set; }
    }

    public class ContactProperty
    {
        [JsonProperty(PropertyName = "property")]
        public string Property { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}