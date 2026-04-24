namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Represents the outcome of an audited business operation.
/// </summary>
public enum AuditOutcome
{
    /// <summary>The operation completed successfully.</summary>
    Success,

    /// <summary>The operation failed due to an error.</summary>
    Failure,

    /// <summary>The operation was rejected by a business rule or policy.</summary>
    Rejected
}
