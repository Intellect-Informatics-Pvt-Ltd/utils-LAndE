namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Specifies how a sensitive field value should be redacted.
/// </summary>
public enum SensitivityMode
{
    /// <summary>Mask the value, optionally retaining trailing characters.</summary>
    Mask,

    /// <summary>Replace the value with a one-way hash.</summary>
    Hash,

    /// <summary>Completely remove the value from output.</summary>
    Redact
}
