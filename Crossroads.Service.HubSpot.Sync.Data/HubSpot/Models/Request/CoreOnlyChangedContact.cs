
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    /// <summary>
    /// Represents the scenario when an existing contact changed something about
    /// their metadata other than email address.
    /// </summary>
    public class CoreOnlyChangedContact : IUpdateContact
    {
        [JsonIgnore]
        public string Email { get; set; }

        public List<ContactProperty> Properties { get; set; }

        [JsonIgnore]
        public EmailAddressCreatedContact ContactDoesNotExistContingency { get; set; }
    }
}