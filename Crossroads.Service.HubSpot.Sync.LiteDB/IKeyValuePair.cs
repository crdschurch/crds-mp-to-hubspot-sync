
using System;

namespace Crossroads.Service.HubSpot.Sync.LiteDb
{
    /// <summary>
    /// Construct establishes a pattern for key/value pair persistence and retrieval from the LiteDb document
    /// data store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    public interface IKeyValuePair<out T, out TU>
    {
        T Key { get; }
        TU Value { get; }
        DateTime LastUpdated { get; set; }
    }
}
