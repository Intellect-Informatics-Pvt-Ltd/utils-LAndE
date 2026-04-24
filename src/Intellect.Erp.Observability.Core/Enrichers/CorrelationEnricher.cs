using Intellect.Erp.Observability.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with the current correlation ID from <see cref="ICorrelationContextAccessor"/>.
/// </summary>
public sealed class CorrelationEnricher : ILogEventEnricher
{
    private static long _errorCount;

    private readonly ICorrelationContextAccessor _accessor;

    public CorrelationEnricher(ICorrelationContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var correlationId = _accessor.CorrelationId;
            if (correlationId is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("correlationId", correlationId));
            }
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
