
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class UpdateByEmailContact : SerialContact
    {
        [JsonIgnore]
        public string Email { get; set; }
    }
}