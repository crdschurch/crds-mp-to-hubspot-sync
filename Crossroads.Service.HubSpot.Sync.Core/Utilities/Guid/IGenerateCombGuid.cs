using System;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid
{
    /// <summary>
    /// Returns a comb guid. This is mostly for mocking purposes.
    /// </summary>
    public interface IGenerateCombGuid
    {
        System.Guid Generate();
    }
}
