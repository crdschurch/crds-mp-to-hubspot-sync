
using System;

namespace Crossroads.Service.HubSpot.Sync.LiteDb
{
    /// <summary>
    /// Construct establishes a pattern for data that ought to be included in every document
    /// persisted to a data store.
    /// </summary>
    public interface IPersist<out T>
    {
        T Id { get; }

        DateTime LastUpdated { get; set; }
    }
}
