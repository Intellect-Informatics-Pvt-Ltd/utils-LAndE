using Intellect.Erp.Observability.Abstractions;
using Serilog;
using Serilog.Context;

namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Audit hook that writes <see cref="AuditEvent"/> as a structured Serilog log entry
/// at Information level with an <c>audit.v1=true</c> tag.
/// </summary>
public sealed class LogOnlyAuditHook : IAuditHook
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LogOnlyAuditHook"/> using the provided Serilog logger.
    /// </summary>
    /// <param name="logger">The Serilog logger to write audit events to.</param>
    public LogOnlyAuditHook(ILogger logger)
    {
        _logger = logger?.ForContext("audit.v1", true)
            ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LogOnlyAuditHook"/> using the static Serilog <see cref="Log"/> logger.
    /// </summary>
    public LogOnlyAuditHook()
        : this(Log.Logger)
    {
    }

    /// <inheritdoc />
    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        _logger.Information(
            "Audit event {EventId}: {Module}.{Feature}.{Operation} by {Actor} on {EntityType}/{EntityId} — {Outcome}",
            auditEvent.EventId,
            auditEvent.Module,
            auditEvent.Feature,
            auditEvent.Operation,
            auditEvent.Actor,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.Outcome);

        return Task.CompletedTask;
    }
}
