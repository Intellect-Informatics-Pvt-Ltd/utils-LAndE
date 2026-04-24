namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Applies a custom regex-based masking rule to a property or field.
/// The <see cref="IRedactionEngine"/> replaces regex matches with the specified replacement string.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field,
    Inherited = true, AllowMultiple = false)]
public sealed class MaskAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MaskAttribute"/>.
    /// </summary>
    /// <param name="regex">The regex pattern to match sensitive portions of the value.</param>
    /// <param name="replacement">The replacement string for matched portions. Defaults to <c>"***"</c>.</param>
    public MaskAttribute(string regex, string replacement = "***")
    {
        Regex = regex;
        Replacement = replacement;
    }

    /// <summary>Gets the regex pattern.</summary>
    public string Regex { get; }

    /// <summary>Gets the replacement string.</summary>
    public string Replacement { get; }
}
