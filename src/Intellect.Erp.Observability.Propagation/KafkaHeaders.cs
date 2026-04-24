using System.Diagnostics;
using System.Text;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Propagation;

/// <summary>
/// Static helpers for writing and reading correlation context to/from Kafka message headers.
/// Header values are stored as UTF-8 byte arrays.
/// </summary>
public static class KafkaHeaders
{
    /// <summary>Header key for the correlation ID.</summary>
    public const string CorrelationIdKey = "correlationId";

    /// <summary>Header key for the causation ID.</summary>
    public const string CausationIdKey = "causationId";

    /// <summary>Header key for the W3C traceparent.</summary>
    public const string TraceparentKey = "traceparent";

    /// <summary>Header key for the tenant ID.</summary>
    public const string TenantIdKey = "tenantId";

    /// <summary>Header key for the user ID.</summary>
    public const string UserIdKey = "userId";

    /// <summary>
    /// Writes correlation context values into a Kafka header dictionary as UTF-8 byte arrays.
    /// </summary>
    /// <param name="headers">The mutable header dictionary to write into.</param>
    /// <param name="correlationAccessor">Provides correlation, causation, and traceparent values.</param>
    /// <param name="tenantAccessor">Optional tenant context accessor for tenant ID.</param>
    /// <param name="userId">Optional user ID to include in headers.</param>
    public static void WriteCorrelation(
        IDictionary<string, byte[]> headers,
        ICorrelationContextAccessor correlationAccessor,
        ITenantContextAccessor? tenantAccessor = null,
        string? userId = null)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(correlationAccessor);

        WriteIfNotNull(headers, CorrelationIdKey, correlationAccessor.CorrelationId);
        WriteIfNotNull(headers, CausationIdKey, correlationAccessor.CausationId);

        // Build traceparent from Activity.Current if accessor doesn't have one
        var traceparent = correlationAccessor.TraceParent;
        if (string.IsNullOrEmpty(traceparent))
        {
            var activity = Activity.Current;
            if (activity is not null)
            {
                var traceId = activity.TraceId.ToHexString();
                var spanId = activity.SpanId.ToHexString();
                var flags = ((int)activity.ActivityTraceFlags).ToString("x2");
                traceparent = $"00-{traceId}-{spanId}-{flags}";
            }
        }

        WriteIfNotNull(headers, TraceparentKey, traceparent);
        WriteIfNotNull(headers, TenantIdKey, tenantAccessor?.TenantId);
        WriteIfNotNull(headers, UserIdKey, userId);
    }

    /// <summary>
    /// Reads correlation context values from a Kafka header dictionary.
    /// </summary>
    /// <param name="headers">The read-only header dictionary to read from.</param>
    /// <returns>A tuple containing the extracted context values. Null values indicate missing headers.</returns>
    public static (string? CorrelationId, string? CausationId, string? TraceParent, string? TenantId, string? UserId) ReadCorrelation(
        IReadOnlyDictionary<string, byte[]> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return (
            CorrelationId: ReadString(headers, CorrelationIdKey),
            CausationId: ReadString(headers, CausationIdKey),
            TraceParent: ReadString(headers, TraceparentKey),
            TenantId: ReadString(headers, TenantIdKey),
            UserId: ReadString(headers, UserIdKey)
        );
    }

    private static void WriteIfNotNull(IDictionary<string, byte[]> headers, string key, string? value)
    {
        if (value is not null)
        {
            headers[key] = Encoding.UTF8.GetBytes(value);
        }
    }

    private static string? ReadString(IReadOnlyDictionary<string, byte[]> headers, string key)
    {
        if (headers.TryGetValue(key, out var bytes) && bytes is not null)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }
}
