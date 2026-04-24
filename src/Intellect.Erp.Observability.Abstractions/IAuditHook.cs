namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Extensibility interface for emitting structured audit events.
/// Implementations may route events to Serilog, Traceability, or Kafka
/// depending on the configured audit mode.
/// </summary>
public interface IAuditHook
{
    /// <summary>
    /// Emits a structured audit event asynchronously.
    /// </summary>
    /// <param name="auditEvent">The audit event to emit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
