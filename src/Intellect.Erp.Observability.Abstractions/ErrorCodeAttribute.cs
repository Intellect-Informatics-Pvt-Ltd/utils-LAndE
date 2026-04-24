namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Declares the default error code for a controller action or service method.
/// Used by the error handling pipeline to associate a stable error code with a method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class ErrorCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ErrorCodeAttribute"/>.
    /// </summary>
    /// <param name="code">The error code (e.g., <c>ERP-LOANS-VAL-0001</c>).</param>
    public ErrorCodeAttribute(string code)
    {
        Code = code;
    }

    /// <summary>Gets the error code.</summary>
    public string Code { get; }
}
