using System;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public class ContactProperty
    {
        [JsonProperty(PropertyName = "property")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ContactProperty contactProperty)
            {
                return Name.Equals(contactProperty.Name, StringComparison.InvariantCulture);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCulture.GetHashCode(Name);
        }
    }
}