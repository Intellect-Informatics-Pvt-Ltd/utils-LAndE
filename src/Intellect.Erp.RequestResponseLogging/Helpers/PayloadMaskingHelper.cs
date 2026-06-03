using System.Collections.Generic;
using System.Linq;
using Intellect.Erp.RequestResponseLogging.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Masks sensitive fields in structured request and response payloads.
    /// </summary>
    internal static class PayloadMaskingHelper
    {
        private const string MaskedValue = "***MASKED***";

        /// <summary>
        /// Masks sensitive values in an arbitrary JSON payload string.
        /// </summary>
        public static string MaskJson(string payload, IReadOnlyCollection<string> sensitiveFields)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            try
            {
                var token = JToken.Parse(payload);
                MaskSensitiveTokens(token, sensitiveFields);
                return token.ToString(Formatting.None);
            }
            catch
            {
                return payload;
            }
        }

        /// <summary>
        /// Masks sensitive fields in query string values.
        /// </summary>
        public static string MaskQueryString(string? queryString, IReadOnlyCollection<string> sensitiveFields)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return string.Empty;
            }

            var values = QueryHelpers.ParseQuery(queryString);
            var masked = values.ToDictionary(
                pair => pair.Key,
                pair => sensitiveFields.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)
                    ? MaskedValue
                    : string.Join(",", pair.Value.ToArray()),
                StringComparer.OrdinalIgnoreCase);

            return string.Join("&", masked.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        }

        /// <summary>
        /// Masks sensitive fields in headers.
        /// </summary>
        public static IDictionary<string, string> MaskHeaders(IHeaderDictionary headers, RequestResponseLoggingOptions options)
        {
            var allowedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                if (options.ExcludedHeaders.Any(excluded => string.Equals(excluded, header.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (options.IncludedHeaders.Length > 0 &&
                    !options.IncludedHeaders.Any(included => string.Equals(included, header.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var value = header.Value.ToString();
                allowedHeaders[header.Key] = options.SensitiveFields.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                    ? MaskedValue
                    : value;
            }

            return allowedHeaders;
        }

        /// <summary>
        /// Masks sensitive values in form fields.
        /// </summary>
        public static IDictionary<string, string> MaskFormFields(IFormCollection form, IReadOnlyCollection<string> sensitiveFields)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in form.Keys)
            {
                var fieldValues = form[key].ToArray() ?? Array.Empty<string>();
                values[key] = sensitiveFields.Contains(key, StringComparer.OrdinalIgnoreCase)
                    ? MaskedValue
                    : string.Join(",", fieldValues);
            }

            return values;
        }

        private static void MaskSensitiveTokens(JToken token, IReadOnlyCollection<string> sensitiveFields)
        {
            if (token is JValue)
            {
                return;
            }

            if (token is JObject obj)
            {
                foreach (var child in obj.Properties())
                {
                    if (sensitiveFields.Contains(child.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        child.Value = MaskedValue;
                        continue;
                    }

                    MaskSensitiveTokens(child.Value, sensitiveFields);
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    MaskSensitiveTokens(item, sensitiveFields);
                }
            }
        }
    }
}
