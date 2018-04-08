using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl
{
    public class Sleeper : ISleep
    {
        private readonly ILogger<Sleeper> _logger = null;

        public Sleeper(ILogger<Sleeper> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Sleep the given number of milliseconds
        /// </summary>
        public void Sleep(int numberOfMillisecondsToSleep)
        {
            if (numberOfMillisecondsToSleep <= 0) return;

            _logger.LogInformation($"About to sleep for {numberOfMillisecondsToSleep} ms...");
            Thread.Sleep(numberOfMillisecondsToSleep);
        }
    }
}
