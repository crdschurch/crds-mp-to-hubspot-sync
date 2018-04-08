using System;
using System.Collections.Generic;
using System.Text;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities
{
    /// <summary>
    /// Abstraction for Thread.Sleep
    /// </summary>
    public interface ISleep
    {
        /// <summary>
        /// Sleep the given number of milliseconds
        /// </summary>
        void Sleep(int numberOfMillisecondsToSleep);
    }
}
