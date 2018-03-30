
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.Core.Logging
{
    /// <summary>
    /// Homegrown definitions for Microsoft Logging CoreEvent Id.
    /// </summary>
    public class CoreEvent
    {
        public static EventId Exception = 1000;

        public static class Http
        {
            public static EventId Request = 100;
            public static EventId Post = 101;
        }

        public static class Serialization
        {
            public static EventId Deserialize = 200;
            public static EventId Serialize = 201;
        }

        public static class Mapping
        {
            public static EventId Map = 300;
        }
    }
}