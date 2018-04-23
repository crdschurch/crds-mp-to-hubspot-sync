using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class SerialCreateContact : IContact
    {
        [JsonProperty(PropertyName = "properties")]
        public ISet<ContactProperty> Properties { get; set; }
    }
}