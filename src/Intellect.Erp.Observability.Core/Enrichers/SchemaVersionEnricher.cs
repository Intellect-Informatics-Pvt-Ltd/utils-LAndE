using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with a constant <c>log.schema</c> property set to <c>"v1"</c>.
/// </summary>
public sealed class SchemaVersionEnricher : ILogEventEnricher
{
    private static long _errorCount;

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("log.schema", "v1"));
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
