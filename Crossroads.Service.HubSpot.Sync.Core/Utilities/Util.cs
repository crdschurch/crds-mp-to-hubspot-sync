﻿using log4net;
using System;

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
        /// preclude subsequent processing. Any logging should have either been done higher up
        /// in the stack or in the <param name="catchMethod">catchMethod</param> parameter.
        /// </summary>
        public static void TryCatchSwallow(Action methodToMuzzle, Action catchMethod = null, Action finallyMethod = null)
        {
            try { methodToMuzzle(); } catch { catchMethod?.Invoke(); } finally { finallyMethod?.Invoke(); }
        }

        // TODO: THE FUTURE PLAN AND PURPOSE OF KEEPING THIS AROUND IS TO ENABLE ASYNC, RATE-LIMITED HTTP REQUESTS.
        // TODO: (contd) I BELIEVE THE CURRENT, BLOCKING REQUEST CALLS ARE THE BARRIER TO THE APP STACK BEING ASYNC
        /*
        /// <summary>
        /// https://stackoverflow.com/a/35494197
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemsToTraverse"></param>
        /// <param name="processItems"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        public static void RateLimit<T>(int maxDegreeOfParallelism, List<T> itemsToTraverse, Action<T> processItems)
        {
            Parallel.ForEach(
                itemsToTraverse,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async item =>
                {
                    await Task.WhenAll(
                        Task.Delay(1000),
                        Task.Run(() =>  processItems(item))
                    );
                });

            //Parallel.For(0, )
        }
		*/
    }
}
