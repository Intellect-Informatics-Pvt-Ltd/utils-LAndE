namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Indicates the severity level of an error for alerting and triage purposes.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Informational — no action required.</summary>
    Info,

    /// <summary>Warning — potential issue that may need attention.</summary>
    Warning,

    /// <summary>Error — a failure that needs investigation.</summary>
    Error,

    /// <summary>Critical — a severe failure requiring immediate attention.</summary>
    Critical
}
