using System.Text.Json;

namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Engine for masking and redacting sensitive data through structural path policies,
/// attribute-driven reflection, and regex fallback patterns.
/// All operations work on shallow copies; original objects are never mutated.
/// </summary>
public interface IRedactionEngine
{
    /// <summary>
    /// Redacts a string value by applying configured regex patterns and masking rules.
    /// </summary>
    /// <param name="value">The string value to redact.</param>
    /// <returns>The redacted string.</returns>
    string Redact(string value);

    /// <summary>
    /// Redacts sensitive fields within a JSON element using structural path policies.
    /// </summary>
    /// <param name="element">The JSON element to redact.</param>
    /// <returns>A new <see cref="JsonElement"/> with sensitive fields masked.</returns>
    JsonElement RedactJson(JsonElement element);

    /// <summary>
    /// Redacts values in a property dictionary using configured masking rules.
    /// </summary>
    /// <param name="properties">The dictionary of property names and values to redact.</param>
    /// <returns>A new dictionary with sensitive values masked.</returns>
    IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> properties);

    /// <summary>
    /// Redacts an object's properties using attribute-driven and structural masking.
    /// Returns a new object; the original is never mutated.
    /// </summary>
    /// <param name="obj">The object to redact.</param>
    /// <param name="type">Optional type override for reflection. Defaults to <paramref name="obj"/>'s runtime type.</param>
    /// <returns>A new object with sensitive properties masked.</returns>
    object RedactObject(object obj, Type? type = null);
}
