using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Maps <see cref="AuditEvent"/> from the Observability platform to
/// <see cref="AuditActivityRecord"/> from the Traceability utility.
/// </summary>
public static class TraceabilityAuditAdapter
{
    /// <summary>
    /// Converts an <see cref="AuditEvent"/> to an <see cref="AuditActivityRecord"/>.
    /// </summary>
    /// <param name="auditEvent">The audit event to convert.</param>
    /// <returns>A new <see cref="AuditActivityRecord"/> populated from the audit event.</returns>
    public static AuditActivityRecord ToActivityRecord(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

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
            OccurredAt = auditEvent.OccurredAt
        };
    }
}
