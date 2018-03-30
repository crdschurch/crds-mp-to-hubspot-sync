using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Core.Test
{
    internal class NormalizedDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("date")]
        public System.DateTime Date { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }
    }

    internal class ApiObjectRoot
    {
        [JsonProperty("objects")]
        public List<ApiObject> Objects { get; set; }
    }

    internal class ApiObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("date")]
        public System.DateTime Date { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }
    }
}