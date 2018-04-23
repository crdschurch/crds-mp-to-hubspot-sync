using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class BulkContact : IBulkContact
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ISet<ContactProperty> Properties { get; set; }
    }
}