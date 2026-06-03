namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Classifies request performance for telemetry and diagnostics.
    /// </summary>
    internal static class PerformanceClassificationHelper
    {
        /// <summary>
        /// Classifies elapsed milliseconds into performance tiers.
        /// </summary>
        public static string Classify(long elapsedMilliseconds, int slowRequestThresholdMs)
        {
            if (elapsedMilliseconds >= 5000)
            {
                return "CRITICAL";
            }

            if (elapsedMilliseconds >= slowRequestThresholdMs)
            {
                return "SLOW";
            }

            if (elapsedMilliseconds >= 1000)
            {
                return "DEGRADED";
            }

            return "NORMAL";
        }

        /// <summary>
        /// Indicates whether the provided elapsed time represents a performance issue.
        /// </summary>
        public static bool IsPerformanceIssue(long elapsedMilliseconds, int slowRequestThresholdMs)
        {
            return elapsedMilliseconds >= slowRequestThresholdMs;
        }
    }
}
