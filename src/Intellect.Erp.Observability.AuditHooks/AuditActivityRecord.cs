namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Placeholder record representing an audit activity record for the Traceability utility.
/// This type will be replaced by the actual Traceability package type when available.
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
    public string TenantId { get; set; } = string.Empty;

    /// <summary>PACS identifier.</summary>
    public string PacsId { get; set; } = string.Empty;

    /// <summary>Type of entity affected.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Identifier of the affected entity.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Outcome of the operation (e.g., "Success", "Failure", "Rejected").</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>Error code if the operation failed.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Additional data associated with the activity.</summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>Timestamp when the activity occurred.</summary>
    public DateTimeOffset OccurredAt { get; set; }
}
