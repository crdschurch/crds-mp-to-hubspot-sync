using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public interface IContact
    {
        [JsonProperty(PropertyName = "properties")]
        ISet<ContactProperty> Properties { get; set; }
    }
}
