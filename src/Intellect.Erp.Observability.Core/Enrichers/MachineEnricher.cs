using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with the machine name from <see cref="Environment.MachineName"/>.
/// </summary>
public sealed class MachineEnricher : ILogEventEnricher
{
    private static long _errorCount;

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("machine", Environment.MachineName));
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
