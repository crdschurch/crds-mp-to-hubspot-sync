using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response
{
    public class HubSpotSerialResult
    {
        /// <summary>
        /// No CLUE what the "v" in vid stands for, but
        /// the HubSpot unique identifier for a contact.
        /// </summary>
        [JsonProperty(PropertyName = "vid")]
        public int ContactVid { get; set; }

        /// <summary>
        /// Whether or not the contact was created with the
        /// related action or updated.
        /// </summary>
        [JsonProperty(PropertyName = "isNew")]
        public bool IsNew { get; set; }
    }
}
