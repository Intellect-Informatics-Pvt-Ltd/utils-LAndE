namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Represents a structured audit event emitted for business operations
/// to satisfy financial traceability requirements.
/// </summary>
/// <param name="EventId">Unique identifier for this audit event.</param>
/// <param name="CorrelationId">The correlation ID of the request that triggered this event.</param>
/// <param name="Module">The module name (e.g., "Loans", "FAS").</param>
/// <param name="Feature">The feature name (e.g., "LoanDisbursement").</param>
/// <param name="Operation">The operation name (e.g., "Create", "Approve").</param>
/// <param name="Actor">The user or system identity that performed the operation.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PacsId">The PACS identifier.</param>
/// <param name="EntityType">The type of entity affected (e.g., "Loan", "Account").</param>
/// <param name="EntityId">The identifier of the affected entity.</param>
/// <param name="Outcome">The outcome of the audited operation.</param>
/// <param name="ErrorCode">The error code, if the operation failed.</param>
/// <param name="Data">Additional key-value data associated with the audit event.</param>
/// <param name="OccurredAt">The timestamp when the event occurred.</param>
public sealed record AuditEvent(
    string EventId,
    string CorrelationId,
    string Module,
    string Feature,
    string Operation,
    string Actor,
    string TenantId,
    string PacsId,
    string EntityType,
    string EntityId,
    AuditOutcome Outcome,
    string? ErrorCode,
    Dictionary<string, object?> Data,
    DateTimeOffset OccurredAt);
