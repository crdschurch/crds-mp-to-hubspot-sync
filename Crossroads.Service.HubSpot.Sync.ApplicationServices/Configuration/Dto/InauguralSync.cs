
using System;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto
{
    /// <summary>
    /// Represents the first time we'll sync newly registered Ministry Platform CRM
    /// contacts to our HubSpot CRM instance (for the sake of marketing).
    /// </summary>
    public class InauguralSync
    {
        public DateTime Date { get; set; }
    }
}
