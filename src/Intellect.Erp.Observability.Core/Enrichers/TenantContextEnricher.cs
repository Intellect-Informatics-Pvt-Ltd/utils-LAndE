using Intellect.Erp.Observability.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Core.Enrichers;

/// <summary>
/// Enriches log events with tenant context (tenantId, stateCode, pacsId, branchCode)
/// from <see cref="ITenantContextAccessor"/>.
/// </summary>
public sealed class TenantContextEnricher : ILogEventEnricher
{
    private static long _errorCount;

    private readonly ITenantContextAccessor _accessor;

    public TenantContextEnricher(ITenantContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <summary>Gets the total number of enrichment errors encountered.</summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var tenantId = _accessor.TenantId;
            if (tenantId is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("tenantId", tenantId));
            }

            var stateCode = _accessor.StateCode;
            if (stateCode is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("stateCode", stateCode));
            }

            var pacsId = _accessor.PacsId;
            if (pacsId is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("pacsId", pacsId));
            }

            var branchCode = _accessor.BranchCode;
            if (branchCode is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("branchCode", branchCode));
            }
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}
