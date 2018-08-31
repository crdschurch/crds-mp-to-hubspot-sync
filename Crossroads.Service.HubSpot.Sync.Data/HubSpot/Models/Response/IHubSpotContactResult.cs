using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response
{
    public interface IHubSpotContactResult
    {
        /// <summary>
        /// No CLUE what the "v" in vid stands for, but
        /// the HubSpot unique identifier for a contact.
        /// </summary>
        [JsonProperty(PropertyName = "vid")]
        int ContactVid { get; set; }
    }
}
