namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Wraps <see cref="IMaskingPolicy"/> from the Traceability utility, providing a
/// simple masking interface that delegates path-based masking to the Traceability policy.
/// </summary>
public sealed class TraceabilityMaskingAdapter
{
    private readonly IMaskingPolicy _maskingPolicy;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceabilityMaskingAdapter"/>.
    /// </summary>
    /// <param name="maskingPolicy">The Traceability masking policy to delegate to.</param>
    public TraceabilityMaskingAdapter(IMaskingPolicy maskingPolicy)
    {
        _maskingPolicy = maskingPolicy ?? throw new ArgumentNullException(nameof(maskingPolicy));
    }

    /// <summary>
    /// Masks a value at the given path by delegating to the Traceability <see cref="IMaskingPolicy"/>.
    /// </summary>
    /// <param name="path">The field path (e.g., <c>$.body.password</c>).</param>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value, or <c>null</c> if the policy returns null.</returns>
    public string? Mask(string path, string? value)
    {
        return _maskingPolicy.Mask(path, value);
    }
}
