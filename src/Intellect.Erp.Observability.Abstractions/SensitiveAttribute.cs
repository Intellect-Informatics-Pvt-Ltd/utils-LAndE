namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Marks a property, field, or parameter as containing sensitive data that must be
/// masked by the <see cref="IRedactionEngine"/> before logging.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    Inherited = true, AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="SensitiveAttribute"/>.
    /// </summary>
    /// <param name="mode">The sensitivity mode. Defaults to <see cref="SensitivityMode.Mask"/>.</param>
    /// <param name="keepLast">Number of trailing characters to retain when masking. Defaults to 4.</param>
    public SensitiveAttribute(SensitivityMode mode = SensitivityMode.Mask, int keepLast = 4)
    {
        Mode = mode;
        KeepLast = keepLast;
    }

    /// <summary>Gets the sensitivity mode.</summary>
    public SensitivityMode Mode { get; }

    /// <summary>Gets the number of trailing characters to retain when masking.</summary>
    public int KeepLast { get; }
}
