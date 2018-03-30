using System;

namespace Crossroads.Service.HubSpot.Sync.Core.Time
{
    /// <summary>
    /// Time abstraction.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Returns a DateTime instance representing the current date and time, according to the executing server,
        /// in local format.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Converts a long value self-described as milliseconds since the Unix epoch to a DateTimeOffset instance.
        /// </summary>
        /// <param name="millisecondsSinceUnixEpoch"></param>
        DateTimeOffset ToDateTimeOffsetUtc(long millisecondsSinceUnixEpoch);
    }
}