using System;

namespace Crossroads.Service.HubSpot.Sync.Core.Time.Impl
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTimeOffset ToDateTimeOffsetUtc(long millisecondsSinceUnixEpoch)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(millisecondsSinceUnixEpoch);
        }
    }
}