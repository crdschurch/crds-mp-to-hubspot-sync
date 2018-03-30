
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Logging
{
    /// <summary>
    /// Homegrown definitions for Microsoft Logging ContentAppEvent Id.
    /// </summary>
    public class AppEvent
    {
        public static class Web
        {
            public static EventId HelloWorldEndpoint = 1;
        }

        public static class Svc
        {
        }
    }
}