namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Marks a property, field, or parameter to be completely excluded from log output
/// by the <see cref="IRedactionEngine"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    Inherited = true, AllowMultiple = false)]
public sealed class DoNotLogAttribute : Attribute
{
}
