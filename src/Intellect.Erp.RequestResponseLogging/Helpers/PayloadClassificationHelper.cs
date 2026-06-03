namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Classifies payload sizes into analytics-friendly categories.
    /// </summary>
    internal static class PayloadClassificationHelper
    {
        /// <summary>
        /// Returns a normalized payload category for the supplied byte count.
        /// </summary>
        public static string Classify(long payloadSizeBytes, long largePayloadThresholdBytes)
        {
            if (payloadSizeBytes <= 0)
            {
                return "NONE";
            }

            if (payloadSizeBytes < 100 * 1024)
            {
                return "SMALL";
            }

            if (payloadSizeBytes < 500 * 1024)
            {
                return "MEDIUM";
            }

            if (payloadSizeBytes < largePayloadThresholdBytes)
            {
                return "LARGE";
            }

            return "VERY_LARGE";
        }
    }
}
