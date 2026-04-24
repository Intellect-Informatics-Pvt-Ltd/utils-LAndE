using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Audit hook that adapts <see cref="AuditEvent"/> to <see cref="AuditActivityRecord"/>
/// and routes it through the Traceability utility's <see cref="ITraceSink"/>.
/// </summary>
public sealed class TraceabilityBridgeAuditHook : IAuditHook
{
    private readonly ITraceSink _traceSink;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceabilityBridgeAuditHook"/>.
    /// </summary>
    /// <param name="traceSink">The trace sink to route audit records to.</param>
    public TraceabilityBridgeAuditHook(ITraceSink traceSink)
    {
        _traceSink = traceSink ?? throw new ArgumentNullException(nameof(traceSink));
    }

    /// <inheritdoc />
    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var record = MapToActivityRecord(auditEvent);
        return _traceSink.RecordAsync(record, cancellationToken);
    }

    /// <summary>
    /// Maps an <see cref="AuditEvent"/> to an <see cref="AuditActivityRecord"/>.
    /// </summary>
    internal static AuditActivityRecord MapToActivityRecord(AuditEvent auditEvent)
    {
        return new AuditActivityRecord
        {
            ActivityId = auditEvent.EventId,
            CorrelationId = auditEvent.CorrelationId,
            Module = auditEvent.Module,
            Feature = auditEvent.Feature,
            Operation = auditEvent.Operation,
            Actor = auditEvent.Actor,
            TenantId = auditEvent.TenantId,
            PacsId = auditEvent.PacsId,
            EntityType = auditEvent.EntityType,
            EntityId = auditEvent.EntityId,
            Outcome = auditEvent.Outcome.ToString(),
            ErrorCode = auditEvent.ErrorCode,
            Data = new Dictionary<string, object?>(auditEvent.Data),
            OccurredAt = auditEvent.OccurredAt
        };
    }
}
