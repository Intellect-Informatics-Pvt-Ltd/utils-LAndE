using Intellect.Erp.RequestResponseLogging.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Provides payload formatting and truncation helpers.
    /// </summary>
    internal static class PayloadFormattingHelper
    {
        /// <summary>
        /// Truncates the payload string if it exceeds configured limits.
        /// </summary>
        public static string TruncatePayload(string payload, RequestResponseLoggingOptions options)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return string.Empty;
            }

            if (options.TruncateLongPayloads && payload.Length > options.MaxPayloadLogLength)
            {
                return payload[..options.MaxPayloadLogLength] + "...[TRUNCATED]";
            }

            return payload;
        }

        /// <summary>
        /// Converts JSON payload into normalized string form.
        /// </summary>
        public static string NormalizeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                var token = JToken.Parse(json);
                return token.ToString(Formatting.None);
            }
            catch
            {
                return json;
            }
        }
    }
}
