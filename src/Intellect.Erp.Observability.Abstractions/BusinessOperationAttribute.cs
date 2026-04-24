namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Declares the business operation context for a controller action or service class.
/// When applied, the <c>BusinessOperationFilter</c> automatically pushes module, feature,
/// and operation into the Serilog <c>LogContext</c> scope.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessOperationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="BusinessOperationAttribute"/>.
    /// </summary>
    /// <param name="module">The module name (e.g., "Loans", "FAS").</param>
    /// <param name="feature">The feature name (e.g., "LoanDisbursement").</param>
    /// <param name="operation">The operation name (e.g., "Create", "Approve").</param>
    public BusinessOperationAttribute(string module, string feature, string operation)
    {
        Module = module;
        Feature = feature;
        Operation = operation;
    }

    /// <summary>Gets the module name.</summary>
    public string Module { get; }

    /// <summary>Gets the feature name.</summary>
    public string Feature { get; }

    /// <summary>Gets the operation name.</summary>
    public string Operation { get; }
}
