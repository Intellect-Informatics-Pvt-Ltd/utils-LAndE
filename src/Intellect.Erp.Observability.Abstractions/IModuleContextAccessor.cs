namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Provides access to the current module and service context for log enrichment.
/// </summary>
public interface IModuleContextAccessor
{
    /// <summary>
    /// Gets the module name (e.g., "Loans", "FAS", "Savings").
    /// </summary>
    string? ModuleName { get; }

    /// <summary>
    /// Gets the service or application name.
    /// </summary>
    string? ServiceName { get; }

    /// <summary>
    /// Gets the deployment environment (e.g., "Development", "Production").
    /// </summary>
    string? Environment { get; }

    /// <summary>
    /// Gets the current feature name (e.g., "LoanDisbursement").
    /// </summary>
    string? Feature { get; }

    /// <summary>
    /// Gets the current operation name (e.g., "Create", "Approve").
    /// </summary>
    string? Operation { get; }
}
