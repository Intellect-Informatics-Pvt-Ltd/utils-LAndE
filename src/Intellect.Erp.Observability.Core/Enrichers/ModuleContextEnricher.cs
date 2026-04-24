using Intellect.Erp.Observability.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with module context (module, serviceName, env, feature, operation)
/// from <see cref="IModuleContextAccessor"/>.
/// </summary>
public sealed class ModuleContextEnricher : ILogEventEnricher
{
    private static long _errorCount;

    private readonly IModuleContextAccessor _accessor;

    public ModuleContextEnricher(IModuleContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var moduleName = _accessor.ModuleName;
            if (moduleName is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("module", moduleName));
            }

            var serviceName = _accessor.ServiceName;
            if (serviceName is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("serviceName", serviceName));
            }

            var env = _accessor.Environment;
            if (env is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("env", env));
            }

            var feature = _accessor.Feature;
            if (feature is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("feature", feature));
            }

            var operation = _accessor.Operation;
            if (operation is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("operation", operation));
            }
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
