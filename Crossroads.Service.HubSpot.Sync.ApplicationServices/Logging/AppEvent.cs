
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
            public static EventId SyncMpContactsToHubSpot = 2;
            public static EventId ViewSyncActivity = 3;
            public static EventId ResetJobProcessingState = 4;
            public static EventId ViewMostRecentSyncActivity = 5;
            public static EventId ViewAllSyncActivities = 6;
            public static EventId ViewJobProcessingState = 7;
            public static EventId ViewLastSuccessfulSyncDates = 8;
            public static EventId ViewHubSpotApiRequestCount = 9;
            public static EventId SetRegistrationSyncDate = 10;
            public static EventId SetCoreUpdateSyncDate = 11;
        }

        public static class Svc
        {
        }
    }
}