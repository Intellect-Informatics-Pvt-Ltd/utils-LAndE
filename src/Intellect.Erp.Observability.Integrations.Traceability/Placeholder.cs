namespace Intellect.Erp.Observability.Integrations.Traceability;

// Placeholder interfaces for the external Intellect.Erp.Traceability package.
// These will be replaced with actual package references once the Traceability utility is published.

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.ITraceContextAccessor.
/// Provides access to the Traceability utility's context values.
/// </summary>
public interface ITraceContextAccessor
{
    string? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    string? TenantId { get; }
    string? StateCode { get; }
    string? CorrelationId { get; }
    string? BranchCode { get; }
}

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.IMaskingPolicy.
/// Provides path-based masking for sensitive field values.
/// </summary>
public interface IMaskingPolicy
{
    string? Mask(string path, string? value);
}

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.ITraceSink.
/// </summary>
public interface ITraceSink
{
    Task WriteAsync(object record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.AuditActivityRecord.
/// </summary>
public sealed class AuditActivityRecord
{
    /// <summary>Unique identifier for the activity.</summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>Correlation ID linking this activity to a request.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Module that generated the activity.</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Feature within the module.</summary>
    public string Feature { get; set; } = string.Empty;

    /// <summary>Operation performed.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>User or system identity that performed the operation.</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>Tenant identifier.</summary>
    public string? TenantId { get; set; }

    /// <summary>PACS identifier.</summary>
    public string? PacsId { get; set; }

    /// <summary>Entity type affected.</summary>
    public string? EntityType { get; set; }

    /// <summary>Entity identifier.</summary>
    public string? EntityId { get; set; }

    /// <summary>Outcome of the operation (e.g., "Success", "Failure", "Rejected").</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>Error code if the operation failed.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Timestamp when the activity occurred.</summary>
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.IDomainPolicyRejectionException.
/// </summary>
public interface IDomainPolicyRejectionException
{
    string PolicyName { get; }
    string RejectionReason { get; }
}

/// <summary>
/// Placeholder for Intellect.Erp.Traceability.ISagaCompensationException.
/// </summary>
public interface ISagaCompensationException
{
    string SagaId { get; }
    string StepName { get; }
}
