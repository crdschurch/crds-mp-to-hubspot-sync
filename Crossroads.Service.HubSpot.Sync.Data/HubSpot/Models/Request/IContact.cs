using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public interface IContact
    {
        string Email { get; set; }

        [JsonProperty(PropertyName = "properties")]
        List<ContactProperty> Properties { get; set; }
    }
}
