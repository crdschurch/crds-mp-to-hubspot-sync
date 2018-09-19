
using System;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb
{
    /// <summary>
    /// Construct establishes a pattern for key/value pair persistence and retrieval from the Mongo document
    /// data store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    public interface IKeyValuePair<out T, out TU>
    {
        T Key { get; }
        TU Value { get; }
        DateTime LastUpdatedUtc { get; set; }
    }
}
