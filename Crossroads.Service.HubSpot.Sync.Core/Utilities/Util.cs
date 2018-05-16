using log4net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities
{
    public static class Util
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Util));

        /// <summary>
        /// Enables logic to be retried multiple times (6 by default). Typical use case:
        /// For anything crossing boundaries (db, http endpoint).
        /// </summary>
        public static T Retry<T>(Func<T> methodToRetry, int numberOfTimesToRetryOnException = 6)
        {
            for (int index = 0; index < numberOfTimesToRetryOnException; index++)
            {
                try
                {
                    return methodToRetry();
                }
                catch (Exception exc)
                {
                    Logger.Warn(exc);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Syntactic sugar for wrapping a function call in a try catch that will ultimately
        /// swallow the exception. Useful for wrapping around methods whose failure should not
        /// preclude subsequent processing. Any logging should have been done higher up in the
        /// stack.
        /// </summary>
        public static void TryCatchSwallow(Action methodToMuzzle)
        {
            try { methodToMuzzle(); } catch { /* logging has already happened; suppressing so core update process can run */ }
        }

        /// <summary>
        /// https://stackoverflow.com/a/35494197
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// <param name="searches"></param>
        /// <param name="processItems"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        public static void RateLimit<T, TU>(List<T> searches, Func<T, TU> processItems, int maxDegreeOfParallelism = 9)
        {
            Parallel.ForEach(
                searches,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async item =>
                {
                    await Task.WhenAll(
                        Task.Delay(1000),
                        Task.Run(() => { processItems(item); })
                    );
                }
            );
        }
    }
}
